using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface ILiveAppValidator
{
    Task ValidateAsync(DeploymentLogContext log, CancellationToken cancellationToken);
}
