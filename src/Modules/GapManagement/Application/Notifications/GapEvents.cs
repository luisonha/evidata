using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Modules.GapManagement.Application.Notifications;

/// <summary>
/// Eventos de ciclo de vida de una brecha de cumplimiento.
/// Usados como MessageType en el Outbox para routing downstream.
/// </summary>
public static class GapEventTypes
{
    public const string GapCreated       = "gap.created.v1";
    public const string GapAssigned      = "gap.assigned.v1";
    public const string GapProgressed    = "gap.progressed.v1";
    public const string GapBlocked       = "gap.blocked.v1";
    public const string GapResolved      = "gap.resolved.v1";
    public const string GapRiskAccepted  = "gap.risk_accepted.v1";
    public const string GapClosed        = "gap.closed.v1";
}

/// <summary>
/// Payload base para todos los eventos de brecha.
/// </summary>
public sealed record GapEventPayload(
    Guid GapId,
    Guid TenantId,
    string GapTitle,
    GapSeverity Severity,
    GapStatus Status,
    string SourceModule,
    Guid SourceEntityId,
    Guid? OwnerId,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string? Extra = null);
