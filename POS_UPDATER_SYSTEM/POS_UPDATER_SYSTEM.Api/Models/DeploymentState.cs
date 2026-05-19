namespace POS_UPDATER_SYSTEM.Api.Models;

public sealed class DeploymentState
{
    public string CurrentVersion { get; set; } = "0.0.0";

    public DeploymentStatus Status { get; set; } = DeploymentStatus.LIVE;

    public DateTimeOffset? LastCheckTime { get; set; }

    public DateTimeOffset? LastUpdateTime { get; set; }

    public bool IsUpdating { get; set; }

    public string LastKnownGoodVersion { get; set; } = "0.0.0";

    public string? LastError { get; set; }
}
