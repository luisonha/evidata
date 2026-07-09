namespace Evidata.Modules.ProcessingInventory.Application.Notifications;

/// <summary>
/// Tipos de eventos del ciclo de vida de un RAT publicados al Outbox.
/// Consumidos por Evidata.Functions.Notifications.
/// </summary>
public static class RatEventTypes
{
    public const string RatSubmittedForReview = "rat.submitted_for_review.v1";
    public const string RatApproved           = "rat.approved.v1";
    public const string RatRejected           = "rat.rejected.v1";
    public const string RatArchived           = "rat.archived.v1";
}

/// <summary>
/// Payload compartido para todos los eventos RAT.
/// </summary>
public sealed record RatEventPayload(
    Guid   ActivityId,
    Guid   TenantId,
    string ActivityName,
    string Status,
    Guid   ActorId,
    string ActorEmail,
    DateTimeOffset OccurredAt,
    string? Reason = null);
