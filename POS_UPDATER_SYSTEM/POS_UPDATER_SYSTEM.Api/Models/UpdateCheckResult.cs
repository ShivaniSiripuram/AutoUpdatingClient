namespace POS_UPDATER_SYSTEM.Api.Models;

public sealed class UpdateCheckResult
{
    public required string CurrentVersion { get; init; }

    public required string LatestVersion { get; init; }

    public bool IsUpdateAvailable { get; init; }

    public LatestUpdateInfo? Latest { get; init; }

    public string? LogFile { get; init; }
}
