namespace Evidata.Modules.Workflow.Application.Notifications;

/// <summary>
/// Event types for Review lifecycle transitions.
/// Used as MessageType in the Outbox for routing downstream.
/// </summary>
public static class ReviewEventTypes
{
    public const string ReviewApproved = "review.approved.v1";
    public const string ReviewChangesRequested = "review.changes_requested.v1";
    public const string ReviewCancelled = "review.cancelled.v1";
}

/// <summary>
/// Payload for ReviewApproved event.
/// Emitted when a review transitions to Approved status.
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

/// <summary>
/// Payload for ReviewChangesRequested event.
/// Emitted when a review transitions to ChangesRequested status.
/// </summary>
public sealed record ReviewChangesRequestedEventPayload(
    Guid ReviewId,
    Guid TenantId,
    string TargetModule,
    string TargetEntityType,
    Guid TargetEntityId,
    Guid ReviewerId,
    string Comments,
    DateTimeOffset OccurredAt,
    Guid? ActorId = null);

/// <summary>
/// Payload for ReviewCancelled event.
/// Emitted when a review transitions to Cancelled status.
/// </summary>
public sealed record ReviewCancelledEventPayload(
    Guid ReviewId,
    Guid TenantId,
    string TargetModule,
    string TargetEntityType,
    Guid TargetEntityId,
    DateTimeOffset OccurredAt,
    Guid? ActorId = null);
