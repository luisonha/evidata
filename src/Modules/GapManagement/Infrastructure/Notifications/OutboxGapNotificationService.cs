using System.Text.Json;
using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Worker.Outbox.Persistence;

// ReSharper disable once RedundantUsingDirective — GapEventTypes y GapEventPayload viven en este NS

namespace Evidata.Modules.GapManagement.Infrastructure.Notifications;

/// <summary>
/// Implementación de <see cref="IGapNotificationService"/> que encola eventos
/// en el Outbox para procesamiento asíncrono.
///
/// Destino lógico: "notification-queue" (routing configurado en Outbox Worker).
/// Todos los eventos son idempotentes — el Outbox garantiza exactly-once vía
/// el ID único del mensaje.
/// </summary>
public sealed class OutboxGapNotificationService : IGapNotificationService
{
    private readonly IOutboxWriter _outbox;
    private const string Destination = "notification-queue";

    public OutboxGapNotificationService(IOutboxWriter outbox)
    {
        _outbox = outbox;
    }

    public Task NotifyCreatedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapCreated, actorId, ct);

    public Task NotifyAssignedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapAssigned, actorId, ct,
            extra: gap.OwnerId.HasValue ? $"owner:{gap.OwnerId}" : null);

    public Task NotifyProgressedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapProgressed, actorId, ct);

    public Task NotifyBlockedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapBlocked, actorId, ct);

    public Task NotifyResolvedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapResolved, actorId, ct);

    public Task NotifyRiskAcceptedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapRiskAccepted, actorId, ct,
            extra: gap.RiskAcceptanceJustification);

    public Task NotifyClosedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default) =>
        Publish(gap, GapEventTypes.GapClosed, actorId, ct);

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Task Publish(
        ComplianceGap gap,
        string eventType,
        Guid actorId,
        CancellationToken ct,
        string? extra = null)
    {
        var payload = new GapEventPayload(
            gap.Id,
            gap.TenantId,
            gap.Title,
            gap.Severity,
            gap.Status,
            gap.SourceModule,
            gap.SourceEntityId,
            gap.OwnerId,
            DateTimeOffset.UtcNow,
            actorId,
            extra);

        return _outbox.EnqueueAsync(
            gap.TenantId.ToString(),
            Destination,
            eventType,
            JsonSerializer.Serialize(payload),
            correlationId: gap.Id.ToString(),
            ct);
    }
}
