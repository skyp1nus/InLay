using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using H.NotifyIcon;

namespace InLay.App.Tray;

/// <summary>
/// Owns the system tray icon and its context menu. Created and disposed by the application host on the
/// UI thread so the icon is removed cleanly on exit (no ghost icon). Menu commands bind to
/// <see cref="TrayViewModel"/>.
/// </summary>
internal sealed class TrayIconService : IDisposable
{
    private const string IconUri = "pack://application:,,,/InLay.App;component/Assets/InLay.ico";

    private readonly TrayViewModel _viewModel;
    private TaskbarIcon? _taskbarIcon;

    public TrayIconService(TrayViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>Creates the tray icon. Must be called on the UI thread.</summary>
    public void Show()
    {
        if (_taskbarIcon is not null)
        {
            return;
        }

        _taskbarIcon = new TaskbarIcon
        {
            DataContext = _viewModel,
            IconSource = new BitmapImage(new Uri(IconUri, UriKind.Absolute)),
            ContextMenu = BuildContextMenu(),
        };
        _taskbarIcon.SetBinding(
            TaskbarIcon.ToolTipTextProperty,
            new Binding(nameof(TrayViewModel.ToolTip)) { Source = _viewModel });

        _taskbarIcon.ForceCreate();
    }

    private ContextMenu BuildContextMenu()
    {
        ContextMenu menu = new() { DataContext = _viewModel };

        MenuItem pause = new()
        {
            Header = "Pause",
            IsCheckable = true,
            Command = _viewModel.TogglePauseCommand,
        };
        pause.SetBinding(
            MenuItem.IsCheckedProperty,
            new Binding(nameof(TrayViewModel.IsPaused)) { Mode = BindingMode.OneWay });
        menu.Items.Add(pause);

        menu.Items.Add(new MenuItem { Header = "Settings…", Command = _viewModel.ShowSettingsCommand });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Exit", Command = _viewModel.ExitCommand });

        return menu;
    }

    public void Dispose()
    {
        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
    }
}
