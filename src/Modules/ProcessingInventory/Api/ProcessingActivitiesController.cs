using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.ProcessingInventory.Api;

[ApiController]
[Authorize]
[Route("api/processing-activities")]
[Route("api/v1/processing-activities")]
public class ProcessingActivitiesController(
    ListProcessingActivitiesQueryHandler listHandler,
    GetProcessingActivityQueryHandler getHandler,
    CreateProcessingActivityCommandHandler createHandler,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProcessingActivityDto>>> List(
        [FromQuery] string? status, CancellationToken ct)
    {
        ProcessingActivityStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ProcessingActivityStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var result = await listHandler.HandleAsync(currentUser.TenantId, statusFilter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcessingActivityDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(currentUser.TenantId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProcessingActivityDto>> Create(
        [FromBody] CreateProcessingActivityRequest request, CancellationToken ct)
    {
        var cmd = new CreateProcessingActivityCommand(
            currentUser.TenantId, request.Name, request.Description,
            request.Controller, request.Department, currentUser.UserId);

        var result = await createHandler.HandleAsync(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

public record CreateProcessingActivityRequest(
    string Name,
    string? Description = null,
    string? Controller = null,
    string? Department = null);
