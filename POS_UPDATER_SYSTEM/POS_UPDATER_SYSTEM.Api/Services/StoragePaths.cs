using Microsoft.Extensions.Options;
using POS_UPDATER_SYSTEM.Api.Options;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class StoragePaths
{
    public StoragePaths(
    IWebHostEnvironment environment,
    IOptions<UpdaterOptions> options)
    {
        var configuredRoot = options.Value.StorageRoot;

        var projectRoot =
            Directory.GetParent(environment.ContentRootPath)!
                .Parent!
                .Parent!
                .FullName;

        Root = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(projectRoot, configuredRoot);

        Current = Path.Combine(Root, "Current");
        Downloads = Path.Combine(Root, "Downloads");
        Staging = Path.Combine(Root, "Staging");
        Backups = Path.Combine(Root, "Backups");
        Logs = Path.Combine(Root, "Logs");
        Registry = Path.Combine(Root, "Registry");

        DeploymentStateFile =
            Path.Combine(Registry, "deployment-state.json");

        Console.WriteLine($"STORAGE ROOT: {Root}");
    }

    public string Root { get; }

    public string Current { get; }

    public string Downloads { get; }

    public string Staging { get; }

    public string Backups { get; }

    public string Logs { get; }

    public string Registry { get; }

    public string DeploymentStateFile { get; }

    public void EnsureInitialized()
    {
        Directory.CreateDirectory(Current);
        Directory.CreateDirectory(Downloads);
        Directory.CreateDirectory(Staging);
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Registry);
    }
}
