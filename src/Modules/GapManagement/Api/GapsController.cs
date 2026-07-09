using Evidata.Modules.GapManagement.Application.Commands;
using Evidata.Modules.GapManagement.Application.Queries;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.GapManagement.Api;

[ApiController]
[Route("api/gaps")]
[Route("api/v1/gaps")]
public class GapsController(
    ListGapsQueryHandler listHandler,
    GetGapsSummaryQueryHandler summaryHandler,
    CreateGapCommandHandler createHandler,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComplianceGapDto>>> List(
        [FromQuery] string? severity = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await listHandler.HandleAsync(currentUser.TenantId, severity, status, ct);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<GapSummaryDto>> Summary(CancellationToken ct)
    {
        var result = await summaryHandler.HandleAsync(currentUser.TenantId, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ComplianceGapDto>> Create(
        [FromBody] CreateGapRequest req, CancellationToken ct)
    {
        var cmd = new CreateGapCommand(
            currentUser.TenantId, req.SourceModule, req.SourceEntityId,
            req.Title, req.Description, req.Severity,
            currentUser.UserId, req.LegalObligationId, req.DueAt);
        var result = await createHandler.HandleAsync(cmd, ct);
        return CreatedAtAction(nameof(List), new { }, result);
    }
}

public record CreateGapRequest(
    string SourceModule, Guid SourceEntityId,
    string Title, string Description, string Severity = "Medium",
    Guid? LegalObligationId = null, DateTimeOffset? DueAt = null);
