using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace InLay.Core;

/// <summary>
/// Detects keyboard-layout switches without polling the keyboard (docs §4.1). It owns a dedicated
/// background thread that hosts a hidden top-level window plus a message pump — self-contained so the
/// engine stays usable without WPF. Two signals drive it: the shell hook (<c>HSHELL_LANGUAGE</c>) for
/// in-place language changes, and WinEvent focus/foreground hooks to re-read the layout of the newly
/// focused window. Emissions are de-duplicated by LANGID and suppressed while <see cref="AppState"/> is
/// paused. <see cref="LayoutChanged"/> is raised on the pump thread; UI consumers must marshal.
/// </summary>
public sealed class LayoutMonitor : IDisposable
{
    private const string WindowClassName = "InLay.LayoutMonitor.Window";

    /// <summary>Private window message (WM_APP + 1) that asks the pump thread to re-read the layout.</summary>
    private const uint ReEvaluateMessage = 0x8000 + 1;

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

    /// <summary>Creates a monitor bound to <paramref name="appState"/>; call <see cref="Start"/> to begin.</summary>
    public LayoutMonitor(AppState appState)
    {
        _appState = appState;
        _wndProc = WndProc;
        _winEventProc = OnWinEvent;
        _appState.PausedChanged += OnPausedChanged;
    }

    /// <summary>Raised on the pump thread when the active keyboard layout changes (de-duplicated).</summary>
    public event EventHandler<LayoutInfo>? LayoutChanged;

    /// <summary>Starts the pump thread and installs the hooks. Idempotent; blocks until hooks are ready.</summary>
    public void Start()
    {
        if (_pumpThread is not null)
        {
            return;
        }

        _ready.Reset();
        var thread = new Thread(PumpThreadMain)
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
        _foregroundHook?.Dispose();
        _focusHook?.Dispose();
        _ready.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe void PumpThreadMain()
    {
        fixed (char* classNamePtr = WindowClassName)
        {
            var className = new PCWSTR(classNamePtr);
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
        var windowClass = new WNDCLASSEXW
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

        return true;
    }

    private void Cleanup(PCWSTR className)
    {
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
                // On HSHELL_LANGUAGE, lParam carries the HKL of the newly activated layout.
                OnLayoutFromHkl(lParam.Value);
            }

            return default;
        }

        switch (msg)
        {
            case ReEvaluateMessage:
                _lastLangId = 0; // force the next resolve to emit
                ReadForegroundLayout();
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
        ReadForegroundLayout();

    private void ReadForegroundLayout()
    {
        HWND foreground = PInvoke.GetForegroundWindow();
        if (foreground.IsNull)
        {
            return;
        }

        uint threadId = PInvoke.GetWindowThreadProcessId(foreground);
        OnLayoutFromHkl(HklToNint(PInvoke.GetKeyboardLayout(threadId)));
    }

    private void OnLayoutFromHkl(nint hkl)
    {
        if (_appState.IsPaused)
        {
            return;
        }

        ushort langId = KeyboardLayoutResolver.LangIdFromHkl(hkl);
        if (langId == 0 || langId == _lastLangId)
        {
            return;
        }

        _lastLangId = langId;
        LayoutChanged?.Invoke(this, KeyboardLayoutResolver.Resolve(hkl));
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
}
