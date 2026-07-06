using System.Windows;
using InLay.App.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace InLay.App;

/// <summary>
/// WPF application entry point. Tray-first background app: no main window is shown; the .NET Generic
/// Host (built in on startup) owns application lifecycle. See docs §3–4.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Content root = the exe directory so appsettings.json is found regardless of the working
        // directory (e.g. under `dotnet run` the CWD is the project folder, not the output folder).
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.AddInLay();

        _host = builder.Build();
        _host.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
