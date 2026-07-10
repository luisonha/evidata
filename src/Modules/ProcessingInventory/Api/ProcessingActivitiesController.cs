using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Application.ViewModels;
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
    UpdateProcessingActivityCommandHandler updateHandler,
    IProcessingActivityControlQueryService controlService,
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

    [HttpGet("{id:guid}/control")]
    [ProducesResponseType(typeof(ProcessingActivityControlViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcessingActivityControlViewModel>> GetControl(
        Guid id,
        CancellationToken ct)
    {
        // P1-FULL-COMPOSITION: Use the injected service which is the composition handler
        // This allows the API layer to override the implementation with full composition
        var result = await controlService.HandleAsync(currentUser.TenantId, id, currentUser.UserId, ct);
        if (result is null)
            return NotFound();

        return Ok(result);
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

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ProcessingActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProcessingActivityDto>> UpdateProcessingActivity(
        Guid id,
        [FromBody] UpdateProcessingActivityRequest request,
        CancellationToken ct)
    {
        var cmd = new UpdateProcessingActivityCommand(
            currentUser.TenantId,
            id,
            currentUser.UserId,
            request.Name,
            request.Description,
            request.Controller,
            request.Department);

        var result = await updateHandler.HandleAsync(cmd, ct);

        return result.Outcome switch
        {
            UpdateProcessingActivityOutcome.Success => Ok(result.Activity),
            UpdateProcessingActivityOutcome.NotFound => NotFound(CreateApiError(result.Error!)),
            UpdateProcessingActivityOutcome.Conflict => Conflict(CreateApiError(result.Error!)),
            UpdateProcessingActivityOutcome.UnprocessableEntity => UnprocessableEntity(CreateApiError(result.Error!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private ApiErrorEnvelope CreateApiError(UpdateProcessingActivityError error) =>
        new(new ApiErrorResponse(
            error.Code,
            error.LabelKey,
            error.Message,
            HttpContext.TraceIdentifier,
            null));
}

public record CreateProcessingActivityRequest(
    string Name,
    string? Description = null,
    string? Controller = null,
    string? Department = null);

public record UpdateProcessingActivityRequest(
    string? Name = null,
    string? Description = null,
    string? Controller = null,
    string? Department = null);
