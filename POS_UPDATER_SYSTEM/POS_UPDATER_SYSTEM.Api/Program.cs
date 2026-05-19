using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using POS_UPDATER_SYSTEM.Api.Models;
using POS_UPDATER_SYSTEM.Api.Options;
using POS_UPDATER_SYSTEM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<UpdaterOptions>(builder.Configuration.GetSection(UpdaterOptions.SectionName));
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpClient();

builder.Services.AddSingleton<StoragePaths>();
builder.Services.AddSingleton<IDeploymentStateStore, DeploymentStateStore>();
builder.Services.AddSingleton<IUpdateClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new UpdateClient(
        factory.CreateClient(nameof(UpdateClient)),
        sp.GetRequiredService<StoragePaths>(),
        sp.GetRequiredService<IOptions<UpdaterOptions>>());
});
builder.Services.AddSingleton<IPackageVerifier, PackageVerifier>();
builder.Services.AddSingleton<IStagingService, StagingService>();
builder.Services.AddSingleton<IActivationService, ActivationService>();
builder.Services.AddSingleton<IRollbackService, RollbackService>();
builder.Services.AddSingleton<IHostRuntimeManager, StaticFileHostRuntimeManager>();
builder.Services.AddSingleton<ILiveAppValidator>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new LiveAppValidator(
        factory.CreateClient(nameof(LiveAppValidator)),
        sp.GetRequiredService<StoragePaths>(),
        sp.GetRequiredService<IOptions<UpdaterOptions>>());
});
builder.Services.AddSingleton<IDeploymentOrchestrator, DeploymentOrchestrator>();
builder.Services.AddHostedService<UpdateWatcherService>();

var app = builder.Build();

var paths = app.Services.GetRequiredService<StoragePaths>();
paths.EnsureInitialized();

var stateStore = app.Services.GetRequiredService<IDeploymentStateStore>();
await stateStore.UpdateAsync(state =>
{
    if (state.IsUpdating || IsTransientStatus(state.Status))
    {
        state.IsUpdating = false;
        state.Status = DeploymentStatus.FAILED;
        state.LastError = "Updater restarted while a deployment transition was in progress.";
    }

    return state;
}, CancellationToken.None);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(paths.Current),
    RequestPath = string.Empty
});

app.UseAuthorization();

app.MapControllers();

app.MapFallback(async context =>
{
    var indexPath = Path.Combine(paths.Current, "index.html");

    Console.WriteLine($"INDEX PATH: {indexPath}");
    if (!File.Exists(indexPath))
{
    Console.WriteLine($"INDEX PATH NOT FOUND: {indexPath}");

    context.Response.StatusCode = StatusCodes.Status404NotFound;

    await context.Response.WriteAsync(
        "No deployed Mini POS app exists in Storage/Current.");

    return;
}

    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();

static bool IsTransientStatus(DeploymentStatus status)
{
    return status is DeploymentStatus.CHECKING
        or DeploymentStatus.DOWNLOADING
        or DeploymentStatus.VERIFYING
        or DeploymentStatus.STAGING
        or DeploymentStatus.VALIDATING
        or DeploymentStatus.BACKING_UP
        or DeploymentStatus.ACTIVATING
        or DeploymentStatus.RESTARTING
        or DeploymentStatus.ROLLBACK;
}
