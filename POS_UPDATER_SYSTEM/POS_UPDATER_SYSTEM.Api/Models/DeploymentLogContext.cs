namespace POS_UPDATER_SYSTEM.Api.Models;

public sealed class DeploymentLogContext
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public DeploymentLogContext(string logFile)
    {
        LogFile = logFile;
    }

    public string LogFile { get; }

    public async Task WriteAsync(string message, CancellationToken cancellationToken)
    {
        var line = $"[{DateTimeOffset.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(LogFile, line, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
