using Evidata.Modules.Evidence.Application.Commands;
using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Evidence.Api;

[ApiController]
[Authorize]
[Route("api/evidence")]
public class EvidenceController(
    ListEvidenceQueryHandler listHandler,
    GetEvidenceQueryHandler getHandler,
    CreateEvidenceCommandHandler createHandler,
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
}

public record CreateEvidenceRequest(
    string Title, string Type, string Sensitivity = "Internal",
    string? Description = null, string? Tags = null);
