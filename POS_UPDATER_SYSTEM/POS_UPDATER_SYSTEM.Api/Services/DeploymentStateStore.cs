using System.Text.Json;
using System.Text.Json.Serialization;
using POS_UPDATER_SYSTEM.Api.Models;

namespace POS_UPDATER_SYSTEM.Api.Services;

public sealed class DeploymentStateStore : IDeploymentStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static DeploymentStateStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly StoragePaths _paths;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DeploymentStateStore(StoragePaths paths)
    {
        _paths = paths;
    }

    public async Task<DeploymentState> GetAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(Func<DeploymentState, DeploymentState> update, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var current = await ReadUnsafeAsync(cancellationToken);
            var next = update(current);
            var json = JsonSerializer.Serialize(next, JsonOptions);
            await File.WriteAllTextAsync(_paths.DeploymentStateFile, json, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<DeploymentState> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        _paths.EnsureInitialized();

        if (!File.Exists(_paths.DeploymentStateFile))
        {
            var initial = new DeploymentState();
            var json = JsonSerializer.Serialize(initial, JsonOptions);
            await File.WriteAllTextAsync(_paths.DeploymentStateFile, json, cancellationToken);
            return initial;
        }

        await using var stream = File.OpenRead(_paths.DeploymentStateFile);
        return await JsonSerializer.DeserializeAsync<DeploymentState>(stream, JsonOptions, cancellationToken)
            ?? new DeploymentState();
    }
}
