using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IRollbackService
{
    Task RollbackAsync(string backupPath, DeploymentLogContext log, CancellationToken cancellationToken);
}
