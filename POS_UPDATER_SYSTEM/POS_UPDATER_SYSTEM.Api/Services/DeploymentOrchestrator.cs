using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class DeploymentOrchestrator : IDeploymentOrchestrator
{
    private readonly IDeploymentStateStore _stateStore;
    private readonly IUpdateClient _updateClient;
    private readonly IPackageVerifier _packageVerifier;
    private readonly IStagingService _stagingService;
    private readonly IActivationService _activationService;
    private readonly IRollbackService _rollbackService;
    private readonly IHostRuntimeManager _hostRuntimeManager;
    private readonly ILiveAppValidator _liveAppValidator;
    private readonly StoragePaths _paths;
    private readonly ILogger<DeploymentOrchestrator> _logger;
    private readonly SemaphoreSlim _deploymentLock = new(1, 1);

    public DeploymentOrchestrator(
        IDeploymentStateStore stateStore,
        IUpdateClient updateClient,
        IPackageVerifier packageVerifier,
        IStagingService stagingService,
        IActivationService activationService,
        IRollbackService rollbackService,
        IHostRuntimeManager hostRuntimeManager,
        ILiveAppValidator liveAppValidator,
        StoragePaths paths,
        ILogger<DeploymentOrchestrator> logger)
    {
        _stateStore = stateStore;
        _updateClient = updateClient;
        _packageVerifier = packageVerifier;
        _stagingService = stagingService;
        _activationService = activationService;
        _rollbackService = rollbackService;
        _hostRuntimeManager = hostRuntimeManager;
        _liveAppValidator = liveAppValidator;
        _paths = paths;
        _logger = logger;
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        await SetStateAsync(DeploymentStatus.CHECKING, cancellationToken, state =>
        {
            state.LastCheckTime = DateTimeOffset.UtcNow;
        });

        var state = await _stateStore.GetAsync(cancellationToken);
        var latest = await _updateClient.GetLatestAsync(cancellationToken);
        var isAvailable = IsNewer(latest.Version, state.CurrentVersion);

        await SetStateAsync(DeploymentStatus.LIVE, cancellationToken);

        return new UpdateCheckResult
        {
            CurrentVersion = state.CurrentVersion,
            LatestVersion = latest.Version,
            IsUpdateAvailable = isAvailable,
            Latest = latest
        };
    }

    public async Task<DeploymentResult> RunDeploymentIfAvailableAsync(CancellationToken cancellationToken)
    {
        if (!await _deploymentLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("Update cycle skipped because another deployment is already running.");
            return new DeploymentResult { Succeeded = false, Message = "Deployment already running." };
        }

        try
        {
            await SetUpdatingAsync(true, cancellationToken);

            var check = await CheckForUpdateAsync(cancellationToken);
            if (!check.IsUpdateAvailable || check.Latest is null)
            {
                return new DeploymentResult
                {
                    Succeeded = true,
                    Message = $"No update available. Current version is {check.CurrentVersion}."
                };
            }

            return await RunDeploymentCoreAsync(check.Latest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment cycle failed.");
            await MarkFailedAsync(ex.Message, cancellationToken);
            return new DeploymentResult { Succeeded = false, Message = ex.Message };
        }
        finally
        {
            await SetUpdatingAsync(false, CancellationToken.None);
            _deploymentLock.Release();
        }
    }

    public async Task<DeploymentResult> RunDeploymentAsync(LatestUpdateInfo latest, CancellationToken cancellationToken)
    {
        if (!await _deploymentLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("Manual deployment skipped because another deployment is already running.");
            return new DeploymentResult { Succeeded = false, Message = "Deployment already running." };
        }

        try
        {
            await SetUpdatingAsync(true, cancellationToken);
            return await RunDeploymentCoreAsync(latest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual deployment failed.");
            await MarkFailedAsync(ex.Message, cancellationToken);
            return new DeploymentResult { Succeeded = false, Message = ex.Message };
        }
        finally
        {
            await SetUpdatingAsync(false, CancellationToken.None);
            _deploymentLock.Release();
        }
    }

    private async Task<DeploymentResult> RunDeploymentCoreAsync(LatestUpdateInfo latest, CancellationToken cancellationToken)
    {
        var log = CreateDeploymentLog(latest.Version);
        string? backupPath = null;
        string? previousVersion = null;

        try
        {
            await log.WriteAsync($"Deployment started for version {latest.Version}.", cancellationToken);

            await SetStateAsync(DeploymentStatus.DOWNLOADING, cancellationToken);
            var packagePath = await _updateClient.DownloadPackageAsync(latest, log, cancellationToken);

            await SetStateAsync(DeploymentStatus.VERIFYING, cancellationToken);
            await _packageVerifier.VerifyAsync(packagePath, latest.Sha256, log, cancellationToken);

            await SetStateAsync(DeploymentStatus.STAGING, cancellationToken);
            var stagingPath = await _stagingService.StageAsync(packagePath, log, cancellationToken);

            await SetStateAsync(DeploymentStatus.VALIDATING, cancellationToken);
            await _stagingService.ValidateAsync(stagingPath, log, cancellationToken);

            var state = await _stateStore.GetAsync(cancellationToken);
            previousVersion = state.CurrentVersion;

            await SetStateAsync(DeploymentStatus.BACKING_UP, cancellationToken);
            backupPath = await _activationService.BackupCurrentAsync(previousVersion, log, cancellationToken);

            await _hostRuntimeManager.StopLiveAppAsync(log, cancellationToken);

            await SetStateAsync(DeploymentStatus.ACTIVATING, cancellationToken);
            await _activationService.ActivateAsync(stagingPath, log, cancellationToken);

            await SetStateAsync(DeploymentStatus.RESTARTING, cancellationToken);
            await _hostRuntimeManager.RestartLiveAppAsync(log, cancellationToken);

            await _liveAppValidator.ValidateAsync(log, cancellationToken);

            await _stateStore.UpdateAsync(state =>
            {
                state.CurrentVersion = latest.Version;
                state.LastKnownGoodVersion = latest.Version;
                state.LastUpdateTime = DateTimeOffset.UtcNow;
                state.Status = DeploymentStatus.LIVE;
                state.LastError = null;
                return state;
            }, cancellationToken);

            await log.WriteAsync("Deployment successful.", cancellationToken);

            return new DeploymentResult
            {
                Succeeded = true,
                Message = "Deployment successful.",
                Version = latest.Version,
                LogFile = log.LogFile
            };
        }
        catch (Exception ex) when (backupPath is not null && previousVersion is not null)
        {
            _logger.LogError(ex, "Activation failed. Rolling back to {Version}.", previousVersion);
            await log.WriteAsync($"Deployment failed: {ex.Message}", CancellationToken.None);
            await SetStateAsync(DeploymentStatus.ROLLBACK, CancellationToken.None, state => state.LastError = ex.Message);

            try
            {
                await _rollbackService.RollbackAsync(backupPath, log, CancellationToken.None);
                await _liveAppValidator.ValidateAsync(log, CancellationToken.None);

                await _stateStore.UpdateAsync(state =>
                {
                    state.CurrentVersion = previousVersion;
                    state.LastKnownGoodVersion = previousVersion;
                    state.Status = DeploymentStatus.ROLLED_BACK;
                    state.LastError = ex.Message;
                    return state;
                }, CancellationToken.None);

                await log.WriteAsync("Rollback successful.", CancellationToken.None);

                return new DeploymentResult
                {
                    Succeeded = false,
                    RolledBack = true,
                    Message = $"Deployment failed and rollback restored {previousVersion}. Error: {ex.Message}",
                    Version = previousVersion,
                    LogFile = log.LogFile
                };
            }
            catch (Exception rollbackException)
            {
                _logger.LogCritical(rollbackException, "Rollback failed.");
                await MarkFailedAsync(rollbackException.Message, CancellationToken.None);
                await log.WriteAsync($"Rollback failed: {rollbackException.Message}", CancellationToken.None);

                return new DeploymentResult
                {
                    Succeeded = false,
                    Message = $"Deployment and rollback failed. Deployment error: {ex.Message}. Rollback error: {rollbackException.Message}",
                    LogFile = log.LogFile
                };
            }
        }
    }

    private DeploymentLogContext CreateDeploymentLog(string version)
    {
        _paths.EnsureInitialized();
        var safeVersion = string.Join("_", version.Split(Path.GetInvalidFileNameChars()));
        var logFile = Path.Combine(_paths.Logs, $"deployment-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{safeVersion}.log");
        return new DeploymentLogContext(logFile);
    }

    private async Task SetUpdatingAsync(bool isUpdating, CancellationToken cancellationToken)
    {
        await _stateStore.UpdateAsync(state =>
        {
            state.IsUpdating = isUpdating;
            return state;
        }, cancellationToken);
    }

    private async Task SetStateAsync(DeploymentStatus status, CancellationToken cancellationToken, Action<DeploymentState>? mutate = null)
    {
        await _stateStore.UpdateAsync(state =>
        {
            state.Status = status;
            mutate?.Invoke(state);
            return state;
        }, cancellationToken);
    }

    private async Task MarkFailedAsync(string error, CancellationToken cancellationToken)
    {
        await _stateStore.UpdateAsync(state =>
        {
            state.Status = DeploymentStatus.FAILED;
            state.LastError = error;
            return state;
        }, cancellationToken);
    }

    private static bool IsNewer(string latestVersion, string currentVersion)
    {
        if (Version.TryParse(latestVersion, out var latest) && Version.TryParse(currentVersion, out var current))
        {
            return latest > current;
        }

        return string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
    }
}
