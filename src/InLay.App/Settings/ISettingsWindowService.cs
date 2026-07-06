namespace InLay.App.Settings;

/// <summary>
/// Shows the settings window. Both the tray "Settings" command and a second-launch reactivation call
/// this single entry point, so there is exactly one code path to the settings UI.
/// </summary>
internal interface ISettingsWindowService
{
    /// <summary>Shows the settings window, or brings it to the front if it is already open. Thread-safe.</summary>
    void Show();
}
