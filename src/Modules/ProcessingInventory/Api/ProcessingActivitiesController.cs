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
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Application.DTOs;

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
    ApproveProcessingActivityCommandHandler approveHandler,
    IProcessingActivityControlQueryService controlService,
    ITimelineQueryService timelineQueryService,
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

    /// <summary>
    /// Get paginated timeline of audit events for a specific ProcessingActivity.
    /// Implements P1-010: TimelineEvent as a read model projection of AuditLog.
    /// 
    /// Timeline events represent the 10 critical auditable actions:
    /// CreateProcessingActivity, UpdateNode, SubmitForReview, Approve, Activate, Archive,
    /// ValidateEvidence, RejectEvidence, AcceptGapWithRisk, GenerateOfficialExport.
    /// </summary>
    /// <param name="id">ProcessingActivity identifier</param>
    /// <param name="skip">Number of events to skip (0-based pagination)</param>
    /// <param name="take">Number of events to take (default: 50, max: 500)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of TimelineEventViewModel ordered by OccurredAt descending</returns>
    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(TimelineEventViewModelEnvelope), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TimelineEventViewModelEnvelope>> GetTimeline(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        // Pagination validation
        if (skip < 0)
            return BadRequest(CreateApiError("INVALID_SKIP", "query.pagination.skip.negative", "Skip must be >= 0"));

        if (take <= 0 || take > 500)
            return BadRequest(CreateApiError("INVALID_TAKE", "query.pagination.take.invalid", "Take must be between 1 and 500"));

        try
        {
            // Tenant isolation: query service enforces tenant isolation internally
            var events = await timelineQueryService.GetTimelineAsync(
                currentUser.TenantId,
                id,
                resource: "ProcessingActivity",
                skip,
                take,
                ct);

            return Ok(new TimelineEventViewModelEnvelope(events));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
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

    /// <summary>
    /// Approve a ProcessingActivity for formal compliance.
    /// 
    /// P1-013: Approves a ProcessingActivity version from UnderReview to Approved state.
    /// 
    /// Authorization: User must have ApproveProcessingActivity permission and cannot be ProcessOwner
    /// if attempting to approve their own activity (SEC-APP-001).
    /// 
    /// Business Rules:
    /// - Version must be in UnderReview state
    /// - No critical gaps or missing evidence
    /// - ProcessingActivity.Flags.BlocksApproval must be false
    /// 
    /// Response:
    /// - 200 OK: Approval succeeded
    /// - 403 Forbidden: User lacks permission or is blocked (SEC-APP-001)
    /// - 404 Not Found: ProcessingActivity not found
    /// - 422 Unprocessable Entity: Business blocker (CriticalGapOpen, BlockingEvidenceMissing, etc.)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ProcessingActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProcessingActivityDto>> Approve(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            var cmd = new ApproveProcessingActivityCommand(
                currentUser.TenantId,
                id,
                currentUser.UserId);

            var result = await approveHandler.HandleAsync(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateApiError("NOT_FOUND", "error.processingActivity.notFound", ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ApprovalBlocked"))
        {
            // Extract blocker code from exception message
            var message = ex.Message;
            var blockerCode = message.Contains("CriticalGapOpen") ? "CriticalGapOpen"
                : message.Contains("BlockingEvidence") ? "BlockingEvidenceMissing"
                : message.Contains("RequiredReview") ? "RequiredReviewPending"
                : message.Contains("Modified") ? "VersionModifiedAfterReview"
                : "ApprovalBlocked";

            return UnprocessableEntity(CreateApiError(
                blockerCode,
                $"error.approval.{blockerCode.ToLowerInvariant()}",
                message));
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                CreateApiError("INTERNAL_ERROR", "error.internal", ex.Message));
        }
    }

    private ApiErrorEnvelope CreateApiError(UpdateProcessingActivityError error) =>
        new(new ApiErrorResponse(
            error.Code,
            error.LabelKey,
            error.Message,
            HttpContext.TraceIdentifier,
            null));

    private ApiErrorEnvelope CreateApiError(string code, string labelKey, string message) =>
       new(new ApiErrorResponse(
           code,
           labelKey,
           message,
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
