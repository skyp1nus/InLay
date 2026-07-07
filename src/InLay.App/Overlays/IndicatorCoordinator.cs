using System.Windows;
using System.Windows.Threading;
using InLay.Core;
using Microsoft.Extensions.Logging;

namespace InLay.App.Overlays;

/// <summary>The indicator presentation modes available in M1 (docs §4.4). A full ModeManager is M4.</summary>
internal enum IndicatorMode
{
    Splash,
    Hud,
    Both,
}

/// <summary>Runtime control of the active indicator mode; backs the tray "Indicator" submenu.</summary>
internal interface IIndicatorController
{
    /// <summary>The currently active indicator mode.</summary>
    IndicatorMode Mode { get; }

    /// <summary>Switches the active mode and reconciles the persistent overlay immediately.</summary>
    void SetMode(IndicatorMode mode);
}

/// <summary>
/// Bridges the UI-free <see cref="LayoutMonitor"/> to the WPF overlays: on each layout change it drives
/// the active mode's overlays (marshalling from the pump thread to the UI thread) and hides them while
/// paused. This is the minimal M1 wiring; the full IIndicatorMode/ModeManager abstraction arrives in M4.
/// </summary>
internal sealed class IndicatorCoordinator : IIndicatorController, IDisposable
{
    private readonly LayoutMonitor _monitor;
    private readonly AppState _appState;
    private readonly FullScreenSplash _splash;
    private readonly CornerHud _hud;
    private readonly ILogger<IndicatorCoordinator> _logger;
    private readonly Dispatcher _dispatcher;

    private IndicatorMode _mode = IndicatorMode.Both;
    private LayoutInfo? _current;
    private bool _started;

    public IndicatorCoordinator(
        LayoutMonitor monitor,
        AppState appState,
        FullScreenSplash splash,
        CornerHud hud,
        ILogger<IndicatorCoordinator> logger)
    {
        _monitor = monitor;
        _appState = appState;
        _splash = splash;
        _hud = hud;
        _logger = logger;
        _dispatcher = Application.Current.Dispatcher;
    }

    /// <inheritdoc/>
    public IndicatorMode Mode => _mode;

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
        _logger.LogInformation("Indicator coordinator started in {Mode} mode.", _mode);
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
    public void SetMode(IndicatorMode mode) => OnUi(() =>
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
        _logger.LogInformation("Indicator mode set to {Mode}.", mode);
        ApplyMode();
    });

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnLayoutChanged(object? sender, LayoutInfo layout) => OnUi(() =>
    {
        _current = layout;
        if (_appState.IsPaused)
        {
            return;
        }

        if (_mode is IndicatorMode.Splash or IndicatorMode.Both)
        {
            _splash.Show(layout);
        }

        if (_mode is IndicatorMode.Hud or IndicatorMode.Both)
        {
            _hud.Show(layout);
        }
    });

    private void OnPausedChanged(object? sender, bool isPaused) => OnUi(() =>
    {
        // On pause, hide everything; on resume the monitor re-emits the current layout, which re-shows.
        if (isPaused)
        {
            _splash.Hide();
            _hud.Hide();
        }
    });

    // Reconciles the persistent HUD to the current mode without triggering a transient splash.
    private void ApplyMode()
    {
        if (_appState.IsPaused)
        {
            return;
        }

        if (_mode is IndicatorMode.Hud or IndicatorMode.Both)
        {
            if (_current is not null)
            {
                _hud.Show(_current);
            }
        }
        else
        {
            _hud.Hide();
        }
    }

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
