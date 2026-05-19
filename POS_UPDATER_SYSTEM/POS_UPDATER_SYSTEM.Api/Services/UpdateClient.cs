using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using POS_UPDATER_SYSTEM.Api.Models;
using POS_UPDATER_SYSTEM.Api.Options;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class UpdateClient : IUpdateClient
{
    private readonly HttpClient _httpClient;
    private readonly StoragePaths _paths;
    private readonly UpdaterOptions _options;

    public UpdateClient(HttpClient httpClient, StoragePaths paths, IOptions<UpdaterOptions> options)
    {
        _httpClient = httpClient;
        _paths = paths;
        _options = options.Value;
    }

    public async Task<LatestUpdateInfo> GetLatestAsync(CancellationToken cancellationToken)
    {
        var latest = await _httpClient.GetFromJsonAsync<LatestUpdateInfo>(_options.LatestJsonUrl, cancellationToken);
        return latest ?? throw new InvalidOperationException("latest.json was empty or invalid.");
    }

    public async Task<string> DownloadPackageAsync(LatestUpdateInfo latest, DeploymentLogContext log, CancellationToken cancellationToken)
    {
        _paths.EnsureInitialized();

        var packageUri = ResolvePackageUri(latest.Package);
        var targetPath = Path.Combine(_paths.Downloads, latest.Package);

        await log.WriteAsync($"Download started: {packageUri}", cancellationToken);

        await using var packageStream = await _httpClient.GetStreamAsync(packageUri, cancellationToken);
        await using var fileStream = File.Create(targetPath);
        await packageStream.CopyToAsync(fileStream, cancellationToken);

        await log.WriteAsync($"Download completed: {targetPath}", cancellationToken);
        return targetPath;
    }

    private Uri ResolvePackageUri(string package)
    {
        if (Uri.TryCreate(package, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        var latestUri = new Uri(_options.LatestJsonUrl, UriKind.Absolute);
        return new Uri(latestUri, package);
    }
}
