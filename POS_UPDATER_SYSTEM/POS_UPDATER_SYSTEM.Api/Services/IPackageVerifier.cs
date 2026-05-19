using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public interface IPackageVerifier
{
    Task VerifyAsync(string packagePath, string expectedSha256, DeploymentLogContext log, CancellationToken cancellationToken);
}
