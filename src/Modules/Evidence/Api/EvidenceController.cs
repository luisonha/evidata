using Evidata.Modules.Evidence.Application.Commands;
using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Evidence.Api;

[ApiController]
[Authorize]
[Route("api/evidence")]
[Route("api/v1/evidence")]
public class EvidenceController(
    ListEvidenceQueryHandler listHandler,
    GetEvidenceQueryHandler getHandler,
    CreateEvidenceCommandHandler createHandler,
    ValidateEvidenceCommandHandler validateHandler,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceDto>>> List(
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await listHandler.HandleAsync(currentUser.TenantId, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(currentUser.TenantId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> Create(
        [FromBody] CreateEvidenceRequest req, CancellationToken ct)
    {
        var cmd = new CreateEvidenceCommand(
            currentUser.TenantId, req.Title, req.Type, req.Sensitivity,
            req.Description, req.Tags, currentUser.UserId);
        var result = await createHandler.HandleAsync(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Validar o rechazar una validación de evidencia (P1-011a).
    /// Audita con AUD-EV-001 (ValidateEvidence) o AUD-EV-002 (RejectEvidence).
    /// SEC-EV-001: Solo el reviewer correcto según ReviewDomain puede validar.
    /// </summary>
    [HttpPost("{validationId:guid}/validate")]
    public async Task<ActionResult<EvidenceValidationResultDto>> ValidateEvidence(
        Guid validationId,
        [FromBody] ValidateEvidenceRequest req,
        CancellationToken ct)
    {
        var cmd = new ValidateEvidenceCommand(
            currentUser.TenantId,
            validationId,
            req.Action,
            req.Comment,
            currentUser.UserId);
        var result = await validateHandler.HandleAsync(cmd, ct);
        return Ok(result);
    }
}

public record CreateEvidenceRequest(
    string Title, string Type, string Sensitivity = "Internal",
    string? Description = null, string? Tags = null);

public record ValidateEvidenceRequest(
    string Action,  // "Validate", "Reject", "MarkInsufficient"
    string? Comment = null);
