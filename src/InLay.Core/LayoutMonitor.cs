using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace InLay.Core;

/// <summary>
/// Detects keyboard-layout switches without hooking the keyboard (docs §4.1). It owns a dedicated
/// background thread that hosts a hidden top-level window plus a message pump — self-contained so the
/// engine stays usable without WPF. Three signals drive it: a steady foreground poll (the primary
/// detector, since the <c>HSHELL_LANGUAGE</c> shell hook is unreliable on modern Windows and often never
/// fires — a LANGID change under an unchanged foreground window is treated as an in-place switch), the
/// shell hook itself when it does fire, and WinEvent focus/foreground hooks to re-read the layout of the
/// newly focused window. Emissions are de-duplicated by LANGID and suppressed while <see cref="AppState"/>
/// is paused. <see cref="LayoutChanged"/> is raised on the pump thread; UI consumers must marshal.
/// </summary>
public sealed class LayoutMonitor : IDisposable
{
    private const string WindowClassName = "InLay.LayoutMonitor.Window";

    /// <summary>Private window message (WM_APP + 1) that asks the pump thread to re-read the layout.</summary>
    private const uint ReEvaluateMessage = 0x8000 + 1;

    /// <summary>
    /// Private window message (WM_APP + 2) carrying an observed (foreground window, HKL) pair — the
    /// foreground window in wParam, the HKL in lParam — from the poll or the debounced focus read.
    /// </summary>
    private const uint PollMessage = 0x8000 + 2;

    // Foreground poll (docs §4.1 item 4). The shell hook (HSHELL_LANGUAGE) is unreliable on modern
    // Windows and often never fires, so this poll — not the hook — is the primary way an in-place switch
    // is noticed. A steady ~150 ms keeps switch-to-indicator latency perceptually instant; since a poll
    // is just GetForegroundWindow + GetKeyboardLayout (trivially cheap), a flat cadence stays effectively
    // 0% CPU at idle, which is why there is no activity-based backoff (that only added worst-case latency
    // exactly when the user returns from idle and switches — the most common moment).
    private const int PollIntervalMs = 150;

    // While the user has paused the indicator nothing is emitted, so the poll idles at a slow cadence.
    private const int PausedPollIntervalMs = 1000;

    // Focus events are read after a short settle delay so transient focus churn (a tray menu, the
    // taskbar, alt-tab) collapses to a single read of the window that actually keeps focus.
    private const int FocusDebounceMs = 200;

    private readonly AppState _appState;
    private readonly WNDPROC _wndProc;
    private readonly WINEVENTPROC _winEventProc;
    private readonly ManualResetEventSlim _ready = new(initialState: false);

    private Thread? _pumpThread;
    private HWND _window;
    private ushort _classAtom;
    private uint _shellHookMessage;
    private UnhookWinEventSafeHandle? _foregroundHook;
    private UnhookWinEventSafeHandle? _focusHook;
    private ushort _lastLangId;
    private LayoutChangeReason _lastReason;

    // The last (foreground window, LANGID) we observed — the baseline the reason classifier compares
    // against to tell a genuine in-place switch (same window, new layout) from a plain refresh. Written
    // only on the pump thread (all observation paths post to it), so no synchronization is needed.
    private nint _lastObservedHwnd;
    private ushort _lastObservedLangId;

    // Poll state — touched only on the (non-overlapping) timer callback, so no synchronization needed.
    private Timer? _pollTimer;
    private ushort _pollLastSeenLangId;
    private Timer? _focusDebounceTimer;

    /// <summary>Creates a monitor bound to <paramref name="appState"/>; call <see cref="Start"/> to begin.</summary>
    public LayoutMonitor(AppState appState)
    {
        _appState = appState;
        _wndProc = WndProc;
        _winEventProc = OnWinEvent;
        _appState.PausedChanged += OnPausedChanged;
    }

    /// <summary>
    /// Raised on the pump thread when the active keyboard layout changes (de-duplicated by LANGID). The
    /// payload carries a <see cref="LayoutChangeReason"/> so consumers can tell a real in-place switch
    /// (<c>HSHELL_LANGUAGE</c>) from a mere refresh (focus change, startup, resume, fallback poll).
    /// </summary>
    public event EventHandler<LayoutChange>? LayoutChanged;

    /// <summary>Starts the pump thread and installs the hooks. Idempotent; blocks until hooks are ready.</summary>
    public void Start()
    {
        if (_pumpThread is not null)
        {
            return;
        }

        _ready.Reset();
        Thread thread = new(PumpThreadMain)
        {
            Name = "InLay.LayoutMonitor",
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        _pumpThread = thread;

        _ready.Wait(TimeSpan.FromSeconds(5));
    }

    /// <summary>Tears down the hooks and stops the pump thread. Idempotent.</summary>
    public void Stop()
    {
        Thread? thread = _pumpThread;
        if (thread is null)
        {
            return;
        }

        if (!_window.IsNull)
        {
            _ = PInvoke.PostMessage(_window, PInvoke.WM_CLOSE, default, default);
        }

        thread.Join(TimeSpan.FromSeconds(2));
        _pumpThread = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _appState.PausedChanged -= OnPausedChanged;
        Stop();
        _pollTimer?.Dispose();
        _focusDebounceTimer?.Dispose();
        _foregroundHook?.Dispose();
        _focusHook?.Dispose();
        _ready.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe void PumpThreadMain()
    {
        fixed (char* classNamePtr = WindowClassName)
        {
            PCWSTR className = new(classNamePtr);
            try
            {
                if (!Initialize(className))
                {
                    return;
                }

                _ready.Set();
                PostReEvaluate(); // emit the current layout so persistent indicators start populated

                while (PInvoke.GetMessage(out MSG msg, default, 0, 0).Value > 0)
                {
                    _ = PInvoke.TranslateMessage(in msg);
                    _ = PInvoke.DispatchMessage(in msg);
                }
            }
            finally
            {
                _ready.Set(); // never leave Start() blocked, even if Initialize threw
                Cleanup(className);
            }
        }
    }

    private unsafe bool Initialize(PCWSTR className)
    {
        WNDCLASSEXW windowClass = new()
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = _wndProc,
            lpszClassName = className,
        };

        _classAtom = PInvoke.RegisterClassEx(in windowClass);
        if (_classAtom == 0)
        {
            return false;
        }

        // Hidden (no WS_VISIBLE) top-level window: shell-hook messages are not delivered to
        // message-only (HWND_MESSAGE) windows, so a real top-level window is required.
        _window = PInvoke.CreateWindowEx(
            WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
            className,
            default,
            WINDOW_STYLE.WS_POPUP,
            0, 0, 0, 0,
            default, default, default, null);
        if (_window.IsNull)
        {
            return false;
        }

        _shellHookMessage = PInvoke.RegisterWindowMessage("SHELLHOOK");
        _ = PInvoke.RegisterShellHookWindow(_window);

        _foregroundHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_SYSTEM_FOREGROUND, PInvoke.EVENT_SYSTEM_FOREGROUND,
            default, _winEventProc, 0, 0, PInvoke.WINEVENT_OUTOFCONTEXT);
        _focusHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_FOCUS, PInvoke.EVENT_OBJECT_FOCUS,
            default, _winEventProc, 0, 0, PInvoke.WINEVENT_OUTOFCONTEXT);

        _pollTimer = new Timer(PollTimerCallback, state: null, PollIntervalMs, Timeout.Infinite);
        _focusDebounceTimer = new Timer(FocusDebounceCallback, state: null, Timeout.Infinite, Timeout.Infinite);

        return true;
    }

    private void Cleanup(PCWSTR className)
    {
        _pollTimer?.Dispose(); // dispose first so a late callback cannot post to a destroyed window
        _pollTimer = null;
        _focusDebounceTimer?.Dispose();
        _focusDebounceTimer = null;

        _foregroundHook?.Dispose(); // UnhookWinEventSafeHandle unhooks on dispose
        _foregroundHook = null;
        _focusHook?.Dispose();
        _focusHook = null;

        if (!_window.IsNull)
        {
            _ = PInvoke.DeregisterShellHookWindow(_window);
            _ = PInvoke.DestroyWindow(_window);
            _window = default;
        }

        if (_classAtom != 0)
        {
            _ = PInvoke.UnregisterClass(className, default);
            _classAtom = 0;
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == _shellHookMessage)
        {
            if ((uint)wParam.Value == PInvoke.HSHELL_LANGUAGE)
            {
                // When it fires at all, HSHELL_LANGUAGE is authoritative for an in-place switch; lParam is
                // the newly activated HKL. Keep the observation baseline in sync so a following poll read of
                // the same layout under the same window is not re-classified as a second switch.
                nint hkl = lParam.Value;
                _lastObservedHwnd = HwndToNint(PInvoke.GetForegroundWindow());
                _lastObservedLangId = KeyboardLayoutResolver.LangIdFromHkl(hkl);
                OnLayoutFromHkl(hkl, LayoutChangeReason.LayoutSwitch);
            }

            return default;
        }

        switch (msg)
        {
            case ReEvaluateMessage:
                // Startup/resume re-read: refresh persistent indicators without flashing the splash. Reset
                // the observation baseline to the current foreground so the next real switch is measured
                // against "now" — never announcing a change that happened before start or while paused.
                _lastLangId = 0; // force the next resolve to emit
                if (TryReadForeground(out nint reHwnd, out nint reHkl))
                {
                    _lastObservedHwnd = reHwnd;
                    _lastObservedLangId = KeyboardLayoutResolver.LangIdFromHkl(reHkl);
                    OnLayoutFromHkl(reHkl, LayoutChangeReason.FocusRefresh);
                }

                return default;
            case PollMessage:
                // The poll and the debounced focus read share this path, each carrying the (foreground
                // window, HKL) pair it observed. The reason classifier decides switch vs refresh from
                // whether the layout changed under the same window; the de-dup then decides whether to emit.
                OnLayoutObserved((nint)wParam.Value, lParam.Value);
                return default;
            case PInvoke.WM_CLOSE:
                PInvoke.PostQuitMessage(0);
                return default;
            default:
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
    }

    private void OnWinEvent(
        HWINEVENTHOOK hook, uint eventId, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime) =>
        ScheduleFocusRead();

    // Restarts the settle timer on every focus/foreground event, so only the window that keeps focus
    // for FocusDebounceMs is read — collapsing transient focus churn (menus, taskbar) into one read.
    private void ScheduleFocusRead()
    {
        Timer? timer = _focusDebounceTimer;
        if (timer is null)
        {
            return;
        }

        try
        {
            _ = timer.Change(FocusDebounceMs, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            // Disposed during teardown between the null check and here — nothing to do.
        }
    }

    private void FocusDebounceCallback(object? state)
    {
        if (TryReadForeground(out nint hwnd, out nint hkl) && !_window.IsNull)
        {
            _ = PInvoke.PostMessage(_window, PollMessage, new WPARAM((nuint)hwnd), new LPARAM(hkl));
        }
    }

    // Reads the foreground window and its active keyboard layout as a consistent pair — the reason
    // classifier needs the HWND the layout belongs to. Returns false for no foreground window, or one of
    // our own (the tray menu / settings): reporting this process's layout would flash the indicator
    // spuriously, most visibly on a tray right-click. Genuine switches still arrive via the poll/hook.
    private static bool TryReadForeground(out nint hwnd, out nint hkl)
    {
        hwnd = 0;
        hkl = 0;

        HWND foreground = PInvoke.GetForegroundWindow();
        if (foreground.IsNull)
        {
            return false;
        }

        uint threadId = PInvoke.GetWindowThreadProcessId(foreground, out uint processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        hwnd = HwndToNint(foreground);
        hkl = HklToNint(PInvoke.GetKeyboardLayout(threadId));
        return true;
    }

    // Runs on a thread-pool thread. It reschedules itself (never overlaps), reads the foreground layout
    // cheaply, and — only when the LANGID actually changed since the last poll — hands the (window, HKL)
    // pair to the pump thread, which classifies switch vs refresh and emits.
    private void PollTimerCallback(object? state)
    {
        Timer? timer = _pollTimer;
        if (timer is null)
        {
            return;
        }

        if (_appState.IsPaused)
        {
            Reschedule(timer, PausedPollIntervalMs);
            return;
        }

        if (TryReadForeground(out nint hwnd, out nint hkl))
        {
            ushort current = KeyboardLayoutResolver.LangIdFromHkl(hkl);
            if (current != 0 && current != _pollLastSeenLangId)
            {
                _pollLastSeenLangId = current;
                if (!_window.IsNull)
                {
                    _ = PInvoke.PostMessage(_window, PollMessage, new WPARAM((nuint)hwnd), new LPARAM(hkl));
                }
            }
        }

        Reschedule(timer, PollIntervalMs);
    }

    private static void Reschedule(Timer timer, int intervalMs)
    {
        try
        {
            _ = timer.Change(intervalMs, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            // The timer was disposed during teardown between the null check and here — nothing to do.
        }
    }

    // Handles a poll/focus observation of the foreground (window, HKL): classifies it as a real in-place
    // switch or a plain refresh, advances the observation baseline, then runs it through the emit de-dup.
    // Always on the pump thread.
    private void OnLayoutObserved(nint hwnd, nint hkl)
    {
        ushort langId = KeyboardLayoutResolver.LangIdFromHkl(hkl);
        LayoutChangeReason reason = ClassifyReason(langId, hwnd, _lastObservedLangId, _lastObservedHwnd);
        _lastObservedHwnd = hwnd;
        _lastObservedLangId = langId;
        OnLayoutFromHkl(hkl, reason);
    }

    /// <summary>
    /// Pure reason classifier, extracted so it can be unit-tested without the message pump. A different
    /// <paramref name="langId"/> under the SAME foreground window (<paramref name="hwnd"/> equals
    /// <paramref name="lastHwnd"/>) is a genuine in-place switch the user made; a different window, an
    /// unchanged layout, or an unresolved one is only a refresh of what we happen to be reading. This is
    /// what lets in-place switches be detected without the unreliable <c>HSHELL_LANGUAGE</c> hook — the
    /// foreground window's HKL simply changing under it is the signal. <paramref name="lastHwnd"/> is 0
    /// until the first observation, so the first read is always a refresh (a real HWND never equals 0).
    /// </summary>
    internal static LayoutChangeReason ClassifyReason(ushort langId, nint hwnd, ushort lastLangId, nint lastHwnd) =>
        langId != 0 && hwnd == lastHwnd && langId != lastLangId
            ? LayoutChangeReason.LayoutSwitch
            : LayoutChangeReason.FocusRefresh;

    private void OnLayoutFromHkl(nint hkl, LayoutChangeReason reason)
    {
        if (_appState.IsPaused)
        {
            return;
        }

        ushort langId = KeyboardLayoutResolver.LangIdFromHkl(hkl);
        if (!ShouldEmit(langId, reason, _lastLangId, _lastReason))
        {
            return;
        }

        _lastLangId = langId;
        _lastReason = reason;
        LayoutChanged?.Invoke(this, new LayoutChange(KeyboardLayoutResolver.Resolve(hkl), reason));
    }

    /// <summary>
    /// Pure de-dup decision for <see cref="OnLayoutFromHkl"/>, extracted so it can be unit-tested without the
    /// Win32 message pump. A different <paramref name="langId"/> always emits. The same layout re-emits only to
    /// "upgrade" a prior <see cref="LayoutChangeReason.FocusRefresh"/> into a <see cref="LayoutChangeReason.LayoutSwitch"/>:
    /// the fallback poll or a focus read can observe an in-place switch's new HKL and report it as a refresh
    /// before the authoritative <c>HSHELL_LANGUAGE</c> arrives, and without this the later real switch would be
    /// de-duped away and the transient splash would never fire. A refresh for an already-reported layout is dropped.
    /// </summary>
    internal static bool ShouldEmit(ushort langId, LayoutChangeReason reason, ushort lastLangId, LayoutChangeReason lastReason)
    {
        if (langId == 0)
        {
            return false;
        }

        if (langId != lastLangId)
        {
            return true;
        }

        return reason == LayoutChangeReason.LayoutSwitch && lastReason != LayoutChangeReason.LayoutSwitch;
    }

    private void OnPausedChanged(object? sender, bool isPaused)
    {
        // On resume, re-read the current layout so persistent indicators refresh immediately.
        if (!isPaused)
        {
            PostReEvaluate();
        }
    }

    private void PostReEvaluate()
    {
        if (!_window.IsNull)
        {
            _ = PInvoke.PostMessage(_window, ReEvaluateMessage, default, default);
        }
    }

    private static unsafe nint HklToNint(HKL hkl) => (nint)hkl.Value;

    private static unsafe nint HwndToNint(HWND hwnd) => (nint)hwnd.Value;
}
