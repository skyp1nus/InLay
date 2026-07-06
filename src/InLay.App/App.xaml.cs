using System.Windows;

namespace InLay.App;

/// <summary>
/// WPF application entry point. Tray-first background app: no main window is shown; the .NET Generic
/// Host (wired in on startup) owns application lifecycle. See docs §3–4.
/// </summary>
public partial class App : Application
{
}
