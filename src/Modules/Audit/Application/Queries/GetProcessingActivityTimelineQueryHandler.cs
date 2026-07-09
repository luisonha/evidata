using System.Text.Json;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Audit.Application.Queries;

/// <summary>
/// Queries the audit log to build timeline events visible in the /control view.
/// Handles model gaps as documented in TimelineEventViewModel.
/// </summary>
public sealed class GetProcessingActivityTimelineQueryHandler(IAuditLogRepository repository)
{
    /// <summary>
    /// Get paginated timeline events for a specific resource (e.g., ProcessingActivity).
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="resourceId">Resource identifier (e.g., processingActivityId)</param>
    /// <param name="resource">Resource type filter (e.g., "ProcessingActivity"). If null, no resource filter applied.</param>
    /// <param name="skip">Number of events to skip (0-based)</param>
    /// <param name="take">Number of events to take (default: 50)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of TimelineEventViewModels ordered by OccurredAt descending</returns>
    public async Task<IReadOnlyList<TimelineEventViewModel>> HandleAsync(
        Guid tenantId,
        Guid resourceId,
        string? resource = "ProcessingActivity",
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(take, 0);

        // Default resource to "ProcessingActivity" if not provided
        var resourceFilter = resource ?? "ProcessingActivity";

        // Query by resource with pagination; repository returns pageable results
        // ordered by OccurredAt descending
        var logs = await repository.GetByResourceAsync(tenantId, resourceFilter, resourceId, ct);

        // Apply skip/take pagination in-memory after fetch (repo doesn't support pagination for GetByResourceAsync yet)
        var paged = logs
            .Skip(skip)
            .Take(take)
            .ToList();

        // Map to ViewModels, filtering out unmappable events (Action doesn't match any AuditEventType)
        var events = new List<TimelineEventViewModel>();
        foreach (var log in paged)
        {
            var dto = MapToViewModel(log);
            if (dto != null)
                events.Add(dto);
        }

        return events;
    }

    /// <summary>
    /// Map AuditLog to TimelineEventViewModel, handling model gaps.
    /// Returns null if Action cannot be parsed to AuditEventType (event excluded from timeline).
    /// </summary>
    private static TimelineEventViewModel? MapToViewModel(AuditLog log)
    {
        // GAP 1: Action is free string, not a closed enum. Try to parse to AuditEventType.
        // If no match, exclude event from timeline (return null).
        // TODO: If many events are filtered out, consider adding a catch-all enum value
        // (e.g., AuditEventType.Unknown) to the ProcessingActivityControlEnums.cs enum.
        if (!Enum.TryParse<ProcessingActivityControlEnums.AuditEventType>(
            log.Action, ignoreCase: true, out var eventType))
        {
            // Action value doesn't match any enum member—exclude this event
            return null;
        }

        var eventTypeString = eventType.ToString();
        var eventTypeLabelKey = $"audit.eventType.{eventTypeString}";

        // GAP 2: AuditLog has NO Result field. AuditSeverity (Info/Warning/Critical)
        // is NOT equivalent to AuditEventResult (Success/Failure/Blocked).
        // Default all events to Success until auditing captures failure semantics.
        // TODO: When AuditLog adds a Result/Status field, map that instead of hardcoding Success.
        const string resultString = nameof(ProcessingActivityControlEnums.AuditEventResult.Success);
        const string resultLabelKey = "audit.result.Success";

        // GAP 3: UserId is Guid? nullable. Contract expects non-nullable actorUserId.
        // System actions (null userId) map to Guid.Empty as placeholder.
        // TODO: Decide if "system actor" (Guid.Empty) should be nullable in contract,
        // or if a canonical "SYSTEM" Guid constant should be defined at domain level.
        var actorUserId = log.UserId ?? Guid.Empty;

        // GAP 4: NO correlationId in AuditLog. Leave as null (optional in contract).
        string? correlationId = null;

        // GAP 5: metadata is optional. Try to deserialize Details as JSON; fall back to null.
        object? metadata = null;
        if (!string.IsNullOrEmpty(log.Details))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<object>(log.Details);
            }
            catch (JsonException)
            {
                // Details is not valid JSON—leave metadata null
            }
        }

        return new TimelineEventViewModel(
            Id: log.Id.ToString(),
            EventType: eventTypeString,
            EventTypeLabelKey: eventTypeLabelKey,
            ResourceType: log.Resource,
            ResourceId: log.ResourceId?.ToString() ?? string.Empty,
            ActorUserId: actorUserId.ToString(),
            OccurredAt: log.OccurredAt.ToString("O"),
            Result: resultString,
            ResultLabelKey: resultLabelKey,
            CorrelationId: correlationId,
            Metadata: metadata
        );
    }
}

/// <summary>
/// Namespace alias for accessing the canonical enums owned by ProcessingInventory.
/// We don't create a project reference—using string-based DTO contract instead.
/// </summary>
internal static class ProcessingActivityControlEnums
{
    // NOTE: These enum values are sourced from
    // src/Modules/ProcessingInventory/Application/ViewModels/ProcessingActivityControlEnums.cs
    // We shadow them locally as strings for the query handler logic and for Enum.TryParse.
    // The DTO returns string values; any normalization to the canonical enum is the frontend's concern.

    public enum AuditEventType
    {
        CreateProcessingActivity,
        UpdateProcessingActivityNode,
        SubmitProcessingActivityForReview,
        ApproveProcessingActivity,
        ActivateProcessingActivity,
        ArchiveProcessingActivity,
        ValidateEvidence,
        RejectEvidence,
        AcceptGapWithRisk,
        GenerateOfficialExport
    }

    public enum AuditEventResult
    {
        Success,
        Failure,
        Blocked
    }
}
