using System.Windows;

namespace InLay.App.Settings;

/// <summary>
/// Owns the single settings window. Creates it on first request; on later requests (tray click or a
/// second-launch reactivation) it restores and brings the existing window to the front instead of
/// opening another. The window's view model is a DI singleton, so toggle state persists across opens.
/// </summary>
internal sealed class SettingsWindowService : ISettingsWindowService
{
    private readonly SettingsViewModel _viewModel;
    private SettingsWindow? _window;

    public SettingsWindowService(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Show()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_window is null)
            {
                _window = new SettingsWindow(_viewModel);
                _window.Closed += OnWindowClosed;
                _window.Show();
                return;
            }

            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }

            _window.Activate();
        });
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_window is not null)
        {
            _window.Closed -= OnWindowClosed;
            _window = null;
        }
    }
}
