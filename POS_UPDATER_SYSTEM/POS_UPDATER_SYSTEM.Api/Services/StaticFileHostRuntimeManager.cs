using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class StaticFileHostRuntimeManager : IHostRuntimeManager
{
    public Task StopLiveAppAsync(DeploymentLogContext log, CancellationToken cancellationToken)
    {
        return log.WriteAsync("Stopping live host boundary. Static hosting remains online while Current is switched.", cancellationToken);
    }

    public Task RestartLiveAppAsync(DeploymentLogContext log, CancellationToken cancellationToken)
    {
        return log.WriteAsync("Restarting live host boundary. Static file middleware will serve the new Current contents.", cancellationToken);
    }
}
