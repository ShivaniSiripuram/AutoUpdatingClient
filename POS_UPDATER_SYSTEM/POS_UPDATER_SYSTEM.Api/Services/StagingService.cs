using System.IO.Compression;
using Microsoft.Extensions.Options;
using POS_UPDATER_SYSTEM.Api.Models;
using POS_UPDATER_SYSTEM.Api.Options;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class StagingService : IStagingService
{
    private readonly StoragePaths _paths;
    private readonly UpdaterOptions _options;

    public StagingService(StoragePaths paths, IOptions<UpdaterOptions> options)
    {
        _paths = paths;
        _options = options.Value;
    }

    public async Task<string> StageAsync(string packagePath, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        var stagingPath = Path.Combine(_paths.Staging, Path.GetFileNameWithoutExtension(packagePath));
        if (Directory.Exists(stagingPath))
        {
            Directory.Delete(stagingPath, recursive: true);
        }

        Directory.CreateDirectory(stagingPath);

        try
        {
            await log.WriteAsync($"Extracting package to staging: {stagingPath}", cancellationToken);
            ExtractToDirectorySafely(packagePath, stagingPath);
            await log.WriteAsync("Extracted to staging.", cancellationToken);
            return stagingPath;
        }
        catch
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }

            throw;
        }
    }

    public async Task ValidateAsync(string stagingPath, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        await log.WriteAsync("Staging validation started.", cancellationToken);

        var root = ResolveApplicationRoot(stagingPath);
        var indexPath = Path.Combine(root, "index.html");

        if (!File.Exists(indexPath))
        {
            throw new InvalidOperationException("Staging validation failed. index.html was not found.");
        }

        var mainScriptExists = Directory.EnumerateFiles(root, "*.js", SearchOption.AllDirectories)
            .Any(path => Path.GetFileName(path).StartsWith("main", StringComparison.OrdinalIgnoreCase));

        if (!mainScriptExists)
        {
            throw new InvalidOperationException($"Staging validation failed. No script matching {_options.MainScriptPattern} was found.");
        }

        var indexHtml = await File.ReadAllTextAsync(indexPath, cancellationToken);
        if (!indexHtml.Contains("<app-root", StringComparison.OrdinalIgnoreCase)
            && !indexHtml.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Staging validation failed. index.html does not look like an Angular application shell.");
        }

        await log.WriteAsync("Validation successful.", cancellationToken);
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

        return candidates.Length == 1
            ? candidates[0]
            : stagingPath;
    }

    private static void ExtractToDirectorySafely(string packagePath, string destinationDirectory)
    {
        var destinationRoot = Path.GetFullPath(destinationDirectory);
        using var archive = ZipFile.OpenRead(packagePath);

        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Package contains an unsafe path: {entry.FullName}");
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}
