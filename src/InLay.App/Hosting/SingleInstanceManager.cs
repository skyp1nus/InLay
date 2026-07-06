using InLay.Core;

namespace InLay.App.Hosting;

/// <summary>
/// Enforces a single running instance per user session using a named <see cref="Mutex"/>. A second
/// launch signals a named <see cref="EventWaitHandle"/> so the primary instance can surface its
/// settings window, then exits. The primary latches onto the event via a thread-pool wait.
/// </summary>
internal sealed class SingleInstanceManager : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;
    private RegisteredWaitHandle? _registration;
    private bool _ownsMutex;

    public SingleInstanceManager()
    {
        _mutex = new Mutex(initiallyOwned: true, ProductInfo.MutexName, out bool createdNew);
        IsPrimaryInstance = createdNew;
        _ownsMutex = createdNew;

        // Opens the existing event when a primary is already running, else creates it.
        _activationEvent = new EventWaitHandle(
            initialState: false,
            mode: EventResetMode.AutoReset,
            name: ProductInfo.ActivationEventName);
    }

    /// <summary>True if this process is the first (primary) instance; false if another is already running.</summary>
    public bool IsPrimaryInstance { get; }

    /// <summary>Called by a secondary instance to ask the primary to come to the foreground.</summary>
    public void SignalPrimaryInstance() => _activationEvent.Set();

    /// <summary>
    /// Registers <paramref name="onActivated"/> to run whenever a secondary instance signals. An
    /// already-latched signal (set before this call) fires immediately, so no activation is lost.
    /// </summary>
    public void RegisterActivationCallback(Action onActivated)
    {
        ArgumentNullException.ThrowIfNull(onActivated);

        _registration = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => onActivated(),
            state: null,
            millisecondsTimeOutInterval: Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public void Dispose()
    {
        _registration?.Unregister(_activationEvent);
        _registration = null;

        _activationEvent.Dispose();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}
