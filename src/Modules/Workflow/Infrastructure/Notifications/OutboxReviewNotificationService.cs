using System.Text.Json;
using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Notifications;
using Evidata.Modules.Workflow.Domain;
using Evidata.Worker.Outbox.Persistence;

// ReSharper disable once RedundantUsingDirective — ReviewEventTypes y ReviewEventPayload viven en este NS

namespace Evidata.Modules.Workflow.Infrastructure.Notifications;

/// <summary>
/// Implementación de <see cref="IReviewNotificationService"/> que encola eventos
/// en el Outbox para procesamiento asíncrono.
///
/// Destino lógico: "notification-queue" (routing configurado en Outbox Worker).
/// Todos los eventos son idempotentes — el Outbox garantiza exactly-once vía
/// el ID único del mensaje.
/// </summary>
public sealed class OutboxReviewNotificationService : IReviewNotificationService
{
    private readonly IOutboxWriter _outbox;
    private const string Destination = "notification-queue";

    public OutboxReviewNotificationService(IOutboxWriter outbox)
    {
        _outbox = outbox;
    }

    public Task NotifyApprovedAsync(Review review, CancellationToken ct = default) =>
        Publish(review, ReviewEventTypes.ReviewApproved, ct);

    public Task NotifyChangesRequestedAsync(Review review, CancellationToken ct = default) =>
        Publish(review, ReviewEventTypes.ReviewChangesRequested, ct);

    public Task NotifyCancelledAsync(Review review, CancellationToken ct = default) =>
        Publish(review, ReviewEventTypes.ReviewCancelled, ct);

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Task Publish(Review review, string eventType, CancellationToken ct)
    {
        object payload = eventType switch
        {
            ReviewEventTypes.ReviewApproved => new ReviewApprovedEventPayload(
                review.Id,
                review.TenantId,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId,
                review.ReviewerId ?? Guid.Empty,
                review.Comments,
                DateTimeOffset.UtcNow,
                review.RequestedBy),
            
            ReviewEventTypes.ReviewChangesRequested => new ReviewChangesRequestedEventPayload(
                review.Id,
                review.TenantId,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId,
                review.ReviewerId ?? Guid.Empty,
                review.Comments ?? string.Empty,
                DateTimeOffset.UtcNow,
                review.RequestedBy),
            
            ReviewEventTypes.ReviewCancelled => new ReviewCancelledEventPayload(
                review.Id,
                review.TenantId,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId,
                DateTimeOffset.UtcNow,
                review.RequestedBy),
            
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };

        return _outbox.EnqueueAsync(
            review.TenantId.ToString(),
            Destination,
            eventType,
            JsonSerializer.Serialize(payload),
            correlationId: review.Id.ToString(),
            ct);
    }
}
