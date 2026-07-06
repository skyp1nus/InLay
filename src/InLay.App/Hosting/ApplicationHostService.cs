using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InLay.App.Hosting;

/// <summary>
/// Owns the application's runtime lifecycle. In M0 it logs start/stop; from commit 9 it creates and
/// disposes the tray icon. It is the single place the UI-visible shell is brought up and torn down.
/// </summary>
internal sealed class ApplicationHostService : IHostedService
{
    private readonly ILogger<ApplicationHostService> _logger;

    public ApplicationHostService(ILogger<ApplicationHostService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InLay host started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InLay host stopping.");
        return Task.CompletedTask;
    }
}
