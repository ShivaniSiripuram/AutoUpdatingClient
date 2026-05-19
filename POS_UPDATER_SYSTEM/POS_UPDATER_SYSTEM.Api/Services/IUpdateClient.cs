using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IUpdateClient
{
    Task<LatestUpdateInfo> GetLatestAsync(CancellationToken cancellationToken);

    Task<string> DownloadPackageAsync(LatestUpdateInfo latest, DeploymentLogContext log, CancellationToken cancellationToken);
}
