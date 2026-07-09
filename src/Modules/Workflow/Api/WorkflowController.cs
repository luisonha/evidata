using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Workflow.Api;

[ApiController]
[Route("api/workflows")]
[Route("api/v1/workflows")]
public class WorkflowController(
    ListWorkflowTasksQueryHandler handler,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskDto>>> List(CancellationToken ct)
    {
        var result = await handler.HandleAsync(currentUser.TenantId, pendingOnly: false, ct);
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskDto>>> Pending(CancellationToken ct)
    {
        var result = await handler.HandleAsync(currentUser.TenantId, pendingOnly: true, ct);
        return Ok(result);
    }
}
