using Microsoft.AspNetCore.Mvc;
using POS_UPDATER_SYSTEM.Api.Models;
using POS_UPDATER_SYSTEM.Api.Services;

namespace POS_UPDATER_SYSTEM.Api.Controllers;

[ApiController]
[Route("api/deployment")]
public sealed class DeploymentController : ControllerBase
{
    private readonly IDeploymentOrchestrator _orchestrator;
    private readonly IDeploymentStateStore _stateStore;

    public DeploymentController(IDeploymentOrchestrator orchestrator, IDeploymentStateStore stateStore)
    {
        _orchestrator = orchestrator;
        _stateStore = stateStore;
    }

    [HttpGet("state")]
    public Task<DeploymentState> GetState(CancellationToken cancellationToken)
    {
        return _stateStore.GetAsync(cancellationToken);
    }

    [HttpPost("check")]
    public Task<UpdateCheckResult> CheckForUpdate(CancellationToken cancellationToken)
    {
        return _orchestrator.CheckForUpdateAsync(cancellationToken);
    }

    [HttpPost("deploy")]
    public Task<DeploymentResult> DeployIfAvailable(CancellationToken cancellationToken)
    {
        return _orchestrator.RunDeploymentIfAvailableAsync(cancellationToken);
    }
}
