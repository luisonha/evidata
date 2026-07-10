namespace Evidata.Modules.Contracts.Notifications;

/// <summary>
/// Payload for ReviewApproved event.
/// Emitted when a review transitions to Approved status.
///
/// P1-019: Moved from Workflow.Application.Notifications to Contracts
/// to enable inter-module event handling without circular dependencies.
/// </summary>
public sealed record ReviewApprovedEventPayload(
    Guid ReviewId,
    Guid TenantId,
    string TargetModule,
    string TargetEntityType,
    Guid TargetEntityId,
    Guid ReviewerId,
    string? Comments,
    DateTimeOffset OccurredAt,
    Guid? ActorId = null);
