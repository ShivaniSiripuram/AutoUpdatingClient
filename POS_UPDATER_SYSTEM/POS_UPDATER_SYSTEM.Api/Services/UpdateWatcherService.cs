using Microsoft.Extensions.Options;
using POS_UPDATER_SYSTEM.Api.Options;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class UpdateWatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UpdaterOptions _options;
    private readonly ILogger<UpdateWatcherService> _logger;

    public UpdateWatcherService(
        IServiceScopeFactory scopeFactory,
        IOptions<UpdaterOptions> options,
        ILogger<UpdateWatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.CheckIntervalMinutes));
        _logger.LogInformation("Background update watcher started. Interval: {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background update cycle failed. The watcher will continue.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IDeploymentOrchestrator>();
        var result = await orchestrator.RunDeploymentIfAvailableAsync(stoppingToken);

        if (result.Succeeded)
        {
            _logger.LogInformation("Background update cycle completed: {Message}", result.Message);
        }
        else
        {
            _logger.LogWarning("Background update cycle completed with no deployment success: {Message}", result.Message);
        }
    }
}
