namespace InLay.Core;

/// <summary>
/// Resolves the on-disk locations InLay uses under <c>%LOCALAPPDATA%\InLay</c>.
/// Path composition is pure (<see cref="Resolve"/>) so it can be unit-tested without touching disk;
/// <see cref="EnsureDirectoriesCreated"/> is the only member with a filesystem side effect.
/// </summary>
public static class AppPaths
{
    private static readonly AppPathSet Current =
        Resolve(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    /// <summary><c>%LOCALAPPDATA%\InLay</c>.</summary>
    public static string LocalAppDataDirectory => Current.LocalAppDataDirectory;

    /// <summary><c>%LOCALAPPDATA%\InLay\logs</c>.</summary>
    public static string LogsDirectory => Current.LogsDirectory;

    /// <summary>Rolling log-file template (Serilog appends the date, e.g. <c>inlay-20260707.log</c>).</summary>
    public static string LogFilePath => Current.LogFilePath;

    /// <summary><c>%LOCALAPPDATA%\InLay\settings.json</c>.</summary>
    public static string SettingsFilePath => Current.SettingsFilePath;

    /// <summary>
    /// Computes every app path under the given <c>%LOCALAPPDATA%</c> base. Pure — no disk access.
    /// </summary>
    /// <param name="localAppDataBase">The Local Application Data root (e.g. <c>C:\Users\me\AppData\Local</c>).</param>
    public static AppPathSet Resolve(string localAppDataBase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppDataBase);

        string root = Path.Combine(localAppDataBase, ProductInfo.Name);
        string logs = Path.Combine(root, "logs");

        return new AppPathSet(
            LocalAppDataDirectory: root,
            LogsDirectory: logs,
            LogFilePath: Path.Combine(logs, "inlay-.log"),
            SettingsFilePath: Path.Combine(root, "settings.json"));
    }

    /// <summary>Creates the app-data and logs directories if they do not yet exist. Call once at startup.</summary>
    public static void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(LocalAppDataDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}

/// <summary>Immutable set of the paths InLay uses under its Local Application Data root.</summary>
public sealed record AppPathSet(
    string LocalAppDataDirectory,
    string LogsDirectory,
    string LogFilePath,
    string SettingsFilePath);
