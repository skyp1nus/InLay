using System.Diagnostics.CodeAnalysis;
using System.Windows;
using InLay.App.Hosting;
using InLay.App.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace InLay.App;

/// <summary>
/// WPF application entry point. Tray-first background app: no main window is shown; the .NET Generic
/// Host (built in on startup) owns application lifecycle. See docs §3–4.
/// </summary>
[SuppressMessage(
    "Reliability",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF owns the Application lifecycle; the host and single-instance manager are disposed in OnExit.")]
public partial class App : Application
{
    private SingleInstanceManager? _singleInstance;
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance gate: a second launch signals the primary to show Settings, then exits.
        _singleInstance = new SingleInstanceManager();
        if (!_singleInstance.IsPrimaryInstance)
        {
            _singleInstance.SignalPrimaryInstance();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        // Content root = the exe directory so appsettings.json is found regardless of the working
        // directory (e.g. under `dotnet run` the CWD is the project folder, not the output folder).
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.AddInLay();

        _host = builder.Build();
        _host.Start();

        // A second launch surfaces this instance's settings window (Show() marshals to the UI thread).
        ISettingsWindowService settingsWindow = _host.Services.GetRequiredService<ISettingsWindowService>();
        _singleInstance.RegisterActivationCallback(settingsWindow.Show);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        _singleInstance?.Dispose();
        _singleInstance = null;

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
