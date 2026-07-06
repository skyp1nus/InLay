using Wpf.Ui.Controls;

namespace InLay.App.Settings;

/// <summary>The settings window (WPF-UI <see cref="FluentWindow"/>). Created on demand by
/// <see cref="SettingsWindowService"/>; closing it simply releases it (the service reopens a fresh one).</summary>
internal sealed partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
