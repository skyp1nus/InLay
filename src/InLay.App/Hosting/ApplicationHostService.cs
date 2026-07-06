using System.Windows;
using InLay.App.Tray;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InLay.App.Hosting;

/// <summary>
/// Owns the application's runtime lifecycle: creates the tray icon on start and disposes it on stop.
/// It is the single place the UI-visible shell is brought up and torn down. Tray creation/disposal is
/// marshalled to the WPF dispatcher because <see cref="IHostedService"/> callbacks are not guaranteed
/// to run on the UI thread.
/// </summary>
internal sealed class ApplicationHostService : IHostedService
{
    private readonly TrayIconService _trayIcon;
    private readonly ILogger<ApplicationHostService> _logger;

    public ApplicationHostService(TrayIconService trayIcon, ILogger<ApplicationHostService> logger)
    {
        _trayIcon = trayIcon;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Application.Current.Dispatcher.Invoke(_trayIcon.Show);
        _logger.LogInformation("InLay host started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InLay host stopping.");
        Application.Current.Dispatcher.Invoke(_trayIcon.Dispose);
        return Task.CompletedTask;
    }
}
