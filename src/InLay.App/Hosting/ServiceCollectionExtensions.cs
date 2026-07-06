using System.Globalization;
using InLay.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace InLay.App.Hosting;

/// <summary>
/// Composition root: wires every InLay service into the Generic Host. Later commits extend this with
/// the tray, autostart, settings, and overlay services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddInLay(this IHostApplicationBuilder builder)
    {
        AppPaths.EnsureDirectoriesCreated();

        // Serilog: minimum level comes from appsettings.json; the file sink path comes from AppPaths
        // (%LOCALAPPDATA% does not expand inside JSON, so the path is set here in code).
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: AppPaths.LogFilePath,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Services.AddSerilog();
        builder.Services.AddHostedService<ApplicationHostService>();

        return builder;
    }
}
