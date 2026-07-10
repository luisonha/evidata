using Evidata.Modules.GapManagement.Application.Commands;
using Evidata.Modules.GapManagement.Application.Queries;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.GapManagement.Api;

[ApiController]
[Authorize]
[Route("api/gaps")]
[Route("api/v1/gaps")]
public class GapsController(
    ListGapsQueryHandler listHandler,
    GetGapsSummaryQueryHandler summaryHandler,
    CreateGapCommandHandler createHandler,
    AcceptGapWithRiskCommandHandler acceptGapHandler,
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

    /// <summary>
    /// Aceptar una brecha con riesgo (P1-011b).
    /// Audita con AUD-GAP-001 (AcceptGapWithRisk).
    /// SEC-GAP-001: Solo TenantOwner/ComplianceAdmin con justificación obligatoria.
    /// </summary>
    [HttpPost("{gapId:guid}/accept-with-risk")]
    public async Task<ActionResult<AcceptGapWithRiskResultDto>> AcceptGapWithRisk(
        Guid gapId,
        [FromBody] AcceptGapWithRiskRequest req,
        CancellationToken ct)
    {
        var cmd = new AcceptGapWithRiskCommand(
            currentUser.TenantId,
            gapId,
            req.Justification,
            currentUser.UserId);
        var result = await acceptGapHandler.HandleAsync(cmd, ct);
        return Ok(result);
    }
}

public record CreateGapRequest(
    string SourceModule, Guid SourceEntityId,
    string Title, string Description, string Severity = "Medium",
    Guid? LegalObligationId = null, DateTimeOffset? DueAt = null);

public record AcceptGapWithRiskRequest(string Justification);
