using System.Text.Json;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Audit.Application.Queries;

/// <summary>
/// Queries the audit log to build timeline events visible in the /control view.
/// Ahora usa el shape formal del contrato P1-009: eventType, result, correlationId, metadata.
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

        // Map to ViewModels, filtering out unmappable events (EventType doesn't match any AuditEventType)
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
    /// Map AuditLog to TimelineEventViewModel.
    /// Returns null if EventType cannot be parsed to enum (event excluded from timeline).
    /// </summary>
    private static TimelineEventViewModel? MapToViewModel(AuditLog log)
    {
        // Parse EventType to canonical enum. If no match, exclude event from timeline.
        if (!Enum.TryParse<ProcessingActivityControlEnums.AuditEventType>(
            log.EventType, ignoreCase: true, out var eventType))
        {
            // EventType value doesn't match any enum member—exclude this event
            return null;
        }

        var eventTypeString = eventType.ToString();
        var eventTypeLabelKey = $"audit.eventType.{eventTypeString}";

        // Map Result field (ahora formal en AuditLog)
        var resultString = log.Result.ToString();
        var resultLabelKey = $"audit.result.{resultString}";

        // UserId es Guid? nullable. Contract espera no-nullable actorUserId.
        // Acciones de sistema (null userId) mapean a Guid.Empty como placeholder.
        var actorUserId = log.UserId ?? Guid.Empty;

        // CorrelationId está disponible en AuditLog (ahora formal en contrato)
        string? correlationId = log.CorrelationId;

        // Metadata es diccionario tipado (JSON serializado). Deserializar.
        object? metadata = null;
        if (!string.IsNullOrEmpty(log.Metadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<object>(log.Metadata);
            }
            catch (JsonException)
            {
                // Metadata is not valid JSON—leave metadata null
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
    // src/Modules/Audit/Domain/AuditEventType.cs (canonical source)
    // We shadow them locally as strings for the query handler logic and for Enum.TryParse.
    // The DTO returns string values; any normalization to the canonical enum is the frontend's concern.

    public enum AuditEventType
    {
        CreateProcessingActivity,
        UpdateNode,
        SubmitForReview,
        Approve,
        Activate,
        Archive,
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
