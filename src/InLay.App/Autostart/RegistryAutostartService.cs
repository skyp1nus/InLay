using InLay.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace InLay.App.Autostart;

/// <summary>
/// Autostart via the per-user <c>HKCU\...\Run</c> registry key (no admin rights required). The value
/// data is the quoted absolute path to the current executable. This is the correct mechanism for a
/// plain (Velopack) build; an MSIX package would instead declare a StartupTask.
/// </summary>
internal sealed class RegistryAutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly ILogger<RegistryAutostartService> _logger;

    public RegistryAutostartService(ILogger<RegistryAutostartService> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ProductInfo.AutostartValueName) is string;
        }
    }

    public void Enable()
    {
        // Environment.ProcessPath is correct even for single-file publish (Assembly.Location is empty there).
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            _logger.LogWarning("Cannot enable autostart: the executable path is unavailable.");
            return;
        }

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ProductInfo.AutostartValueName, $"\"{exePath}\"", RegistryValueKind.String);
        _logger.LogInformation("Autostart enabled for {ExePath}.", exePath);
    }

    public void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ProductInfo.AutostartValueName, throwOnMissingValue: false);
        _logger.LogInformation("Autostart disabled.");
    }
}
