using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IStagingService
{
    Task<string> StageAsync(string packagePath, DeploymentLogContext log, CancellationToken cancellationToken);

    Task ValidateAsync(string stagingPath, DeploymentLogContext log, CancellationToken cancellationToken);
}
