using CommunityToolkit.Mvvm.ComponentModel;
using InLay.App.Autostart;
using Microsoft.Extensions.Logging;

namespace InLay.App.Settings;

/// <summary>Backs the settings window. For M0 its only setting is the "Start with Windows" toggle.</summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAutostartService _autostart;
    private readonly ILogger<SettingsViewModel> _logger;

    // Guards the initial read-from-registry so seeding the toggle does not write it straight back.
    private readonly bool _initialized;

    [ObservableProperty]
    private bool _startWithWindows;

    public SettingsViewModel(IAutostartService autostart, ILogger<SettingsViewModel> logger)
    {
        _autostart = autostart;
        _logger = logger;

        StartWithWindows = _autostart.IsEnabled;
        _initialized = true;
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (!_initialized)
        {
            return;
        }

        if (value)
        {
            _autostart.Enable();
        }
        else
        {
            _autostart.Disable();
        }

        _logger.LogInformation("Start-with-Windows set to {Enabled}.", value);
    }
}
