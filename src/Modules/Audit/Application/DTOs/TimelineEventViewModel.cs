namespace Evidata.Modules.Audit.Application.DTOs;

/// <summary>
/// DTO for timeline events visible to frontend in /control read model.
/// Maps AuditLog domain entities to the UI contract shape.
///
/// Model gaps documented:
/// 1. AuditLog.Action is a free string without formal enum relation.
///    We attempt Enum.TryParse&lt;AuditEventType&gt; against the source enum.
///    If no match, the event is excluded from timeline (not returned by handler).
/// 2. AuditLog has NO Result field. AuditSeverity (Info/Warning/Critical) is NOT
///    semantically equivalent to AuditEventResult (Success/Failure/Blocked).
///    ALL events default to Success until AuditLog captures failure semantics explicitly.
/// 3. AuditLog.UserId is Guid? nullable, but contract expects non-nullable actorUserId.
///    System actions (null userId) map to Guid.Empty as a placeholder for "system actor".
/// 4. NO correlationId in AuditLog model—always left null (optional in contract).
/// 5. metadata is optional—Details JSON deserialized if valid, null otherwise.
/// </summary>
public record TimelineEventViewModel(
    string Id,
    string EventType,
    string EventTypeLabelKey,
    string ResourceType,
    string ResourceId,
    string ActorUserId,
    string OccurredAt,
    string Result,
    string ResultLabelKey,
    string? CorrelationId = null,
    object? Metadata = null);

/// <summary>
/// Wrapper envelope for paginated timeline results.
/// Returns the list of TimelineEventViewModel for /api/v1/processing-activities/{id}/timeline endpoint.
/// </summary>
public record TimelineEventViewModelEnvelope(
    IReadOnlyList<TimelineEventViewModel> Events);
