using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class RollbackService : IRollbackService
{
    private readonly StoragePaths _paths;
    private readonly IHostRuntimeManager _hostRuntimeManager;

    public RollbackService(StoragePaths paths, IHostRuntimeManager hostRuntimeManager)
    {
        _paths = paths;
        _hostRuntimeManager = hostRuntimeManager;
    }

    public async Task RollbackAsync(string backupPath, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(backupPath))
        {
            throw new InvalidOperationException($"Rollback backup does not exist: {backupPath}");
        }

        await log.WriteAsync("Rollback started.", cancellationToken);
        await _hostRuntimeManager.StopLiveAppAsync(log, cancellationToken);

        ActivationService.ClearDirectory(_paths.Current);
        ActivationService.CopyDirectory(backupPath, _paths.Current);

        await _hostRuntimeManager.RestartLiveAppAsync(log, cancellationToken);
        await log.WriteAsync("Rollback restored previous application state.", cancellationToken);
    }
}
