using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InLay.App.Settings;
using InLay.Core;
using Microsoft.Extensions.Logging;

namespace InLay.App.Tray;

/// <summary>
/// Backs the tray icon: its tooltip, the Pause check state, and the Pause / Settings / Exit commands.
/// Paused state lives in <see cref="AppState"/> (the engine seam); this view model mirrors it for the UI.
/// </summary>
internal sealed partial class TrayViewModel : ObservableObject, IDisposable
{
    private readonly AppState _appState;
    private readonly ISettingsWindowService _settingsWindow;
    private readonly ILogger<TrayViewModel> _logger;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _toolTip = ProductInfo.Name;

    public TrayViewModel(AppState appState, ISettingsWindowService settingsWindow, ILogger<TrayViewModel> logger)
    {
        _appState = appState;
        _settingsWindow = settingsWindow;
        _logger = logger;

        _appState.PausedChanged += OnPausedChanged;
        Sync(_appState.IsPaused);
    }

    [RelayCommand]
    private void TogglePause()
    {
        bool paused = _appState.TogglePause();
        _logger.LogInformation("Pause toggled to {IsPaused}.", paused);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        _logger.LogInformation("Settings requested.");
        _settingsWindow.Show();
    }

    [RelayCommand]
    private void Exit()
    {
        // OnExplicitShutdown: this is the single explicit shutdown path. WPF's OnExit then stops the
        // host (which disposes the tray) and flushes logs.
        _logger.LogInformation("Exit requested.");
        Application.Current.Shutdown();
    }

    private void OnPausedChanged(object? sender, bool isPaused) => Sync(isPaused);

    private void Sync(bool isPaused)
    {
        IsPaused = isPaused;
        ToolTip = isPaused ? $"{ProductInfo.Name} — Paused" : ProductInfo.Name;
    }

    public void Dispose() => _appState.PausedChanged -= OnPausedChanged;
}
