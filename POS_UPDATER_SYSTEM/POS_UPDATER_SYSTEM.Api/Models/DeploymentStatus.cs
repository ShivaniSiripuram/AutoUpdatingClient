namespace POS_UPDATER_SYSTEM.Api.Models;

public enum DeploymentStatus
{
    LIVE,
    CHECKING,
    DOWNLOADING,
    VERIFYING,
    STAGING,
    VALIDATING,
    BACKING_UP,
    ACTIVATING,
    RESTARTING,
    ROLLBACK,
    ROLLED_BACK,
    FAILED
}
