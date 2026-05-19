using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class ActivationService : IActivationService
{
    private readonly StoragePaths _paths;

    public ActivationService(StoragePaths paths)
    {
        _paths = paths;
    }

    public async Task<string> BackupCurrentAsync(string currentVersion, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        var backupVersion = string.IsNullOrWhiteSpace(currentVersion) ? "0.0.0" : currentVersion;
        var backupPath = Path.Combine(_paths.Backups, backupVersion, DateTimeOffset.Now.ToString("yyyyMMddHHmmss"));

        Directory.CreateDirectory(backupPath);
        CopyDirectory(_paths.Current, backupPath);

        await log.WriteAsync($"Backup created: {backupPath}", cancellationToken);
        return backupPath;
    }

    public async Task ActivateAsync(string stagingPath, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        await log.WriteAsync("Activating new version.", cancellationToken);

        var applicationRoot = ResolveApplicationRoot(stagingPath);
        ClearDirectory(_paths.Current);
        CopyDirectory(applicationRoot, _paths.Current);

        await log.WriteAsync("Current application state replaced with staged candidate.", cancellationToken);
    }

    private static string ResolveApplicationRoot(string stagingPath)
    {
        if (File.Exists(Path.Combine(stagingPath, "index.html")))
        {
            return stagingPath;
        }

        var candidates = Directory.EnumerateFiles(stagingPath, "index.html", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();

        return candidates.Length == 1 ? candidates[0] : stagingPath;
    }

    internal static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }

    internal static void ClearDirectory(string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            File.Delete(file);
        }

        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            Directory.Delete(childDirectory, recursive: true);
        }
    }
}
