using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IActivationService
{
    Task<string> BackupCurrentAsync(string currentVersion, DeploymentLogContext log, CancellationToken cancellationToken);

    Task ActivateAsync(string stagingPath, DeploymentLogContext log, CancellationToken cancellationToken);
}
