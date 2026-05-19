using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IHostRuntimeManager
{
    Task StopLiveAppAsync(DeploymentLogContext log, CancellationToken cancellationToken);

    Task RestartLiveAppAsync(DeploymentLogContext log, CancellationToken cancellationToken);
}
