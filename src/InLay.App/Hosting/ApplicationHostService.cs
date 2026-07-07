using System.Windows;
using InLay.App.Overlays;
using InLay.App.Tray;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InLay.App.Hosting;

/// <summary>
/// Owns the application's runtime lifecycle: brings up the tray icon and the indicator subsystem on
/// start and tears them down on stop. It is the single place the UI-visible shell is created and
/// destroyed. Tray creation/disposal is marshalled to the WPF dispatcher because
/// <see cref="IHostedService"/> callbacks are not guaranteed to run on the UI thread.
/// </summary>
internal sealed class ApplicationHostService : IHostedService
{
    private readonly TrayIconService _trayIcon;
    private readonly IndicatorCoordinator _indicators;
    private readonly ILogger<ApplicationHostService> _logger;

    public ApplicationHostService(
        TrayIconService trayIcon,
        IndicatorCoordinator indicators,
        ILogger<ApplicationHostService> logger)
    {
        _trayIcon = trayIcon;
        _indicators = indicators;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Application.Current.Dispatcher.Invoke(_trayIcon.Show);
        _indicators.Start();
        _logger.LogInformation("InLay host started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InLay host stopping.");
        _indicators.Stop();
        Application.Current.Dispatcher.Invoke(_trayIcon.Dispose);
        return Task.CompletedTask;
    }
}
