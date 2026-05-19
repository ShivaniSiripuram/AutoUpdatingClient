using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IDeploymentStateStore
{
    Task<DeploymentState> GetAsync(CancellationToken cancellationToken);

    Task UpdateAsync(Func<DeploymentState, DeploymentState> update, CancellationToken cancellationToken);
}
