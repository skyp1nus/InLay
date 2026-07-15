using System.Windows;
using System.Windows.Threading;
using InLay.Core;
using Microsoft.Extensions.Logging;

namespace InLay.App.Overlays;

/// <summary>
/// Bridges the UI-free <see cref="LayoutMonitor"/> to the WPF overlays: on a real layout switch it shows
/// the transient indicator (marshalling from the pump thread to the UI thread) and hides it while paused.
/// InLay's indicators are transient-only — the full-screen splash today, the caret-side badge in M2/M3;
/// there are no persistent modes. Selecting between the transient modes arrives with the caret badge
/// (docs §4.4).
/// </summary>
internal sealed class IndicatorCoordinator : IDisposable
{
    private readonly LayoutMonitor _monitor;
    private readonly AppState _appState;
    private readonly FullScreenSplash _splash;
    private readonly ILogger<IndicatorCoordinator> _logger;
    private readonly Dispatcher _dispatcher;

    private bool _started;

    public IndicatorCoordinator(
        LayoutMonitor monitor,
        AppState appState,
        FullScreenSplash splash,
        ILogger<IndicatorCoordinator> logger)
    {
        _monitor = monitor;
        _appState = appState;
        _splash = splash;
        _logger = logger;
        _dispatcher = Application.Current.Dispatcher;
    }

    /// <summary>Subscribes to layout/pause changes and starts the monitor. Idempotent.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _monitor.LayoutChanged += OnLayoutChanged;
        _appState.PausedChanged += OnPausedChanged;
        _monitor.Start();
        _logger.LogInformation("Indicator coordinator started.");
    }

    /// <summary>Stops the monitor and unsubscribes. Idempotent.</summary>
    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        _monitor.LayoutChanged -= OnLayoutChanged;
        _appState.PausedChanged -= OnPausedChanged;
        _monitor.Stop();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnLayoutChanged(object? sender, LayoutChange e) => OnUi(() =>
    {
        if (_appState.IsPaused)
        {
            return;
        }

        // The splash is transient: fire it only on a real in-place switch, so it never flashes on a focus
        // change, startup, resume, or fallback-poll refresh.
        if (e.Reason == LayoutChangeReason.LayoutSwitch)
        {
            _splash.Show(e.Layout);
        }
    });

    private void OnPausedChanged(object? sender, bool isPaused) => OnUi(() =>
    {
        if (isPaused)
        {
            _splash.Hide();
        }
    });

    private void OnUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = _dispatcher.BeginInvoke(action);
        }
    }
}
