using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Modules.GapManagement.Application.Notifications;

/// <summary>
/// Publica eventos de ciclo de vida de brechas al Outbox.
/// </summary>
public interface IGapNotificationService
{
    /// <summary>Notifica la creación de una brecha.</summary>
    Task NotifyCreatedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica asignación de responsable.</summary>
    Task NotifyAssignedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica inicio de progreso de remediación.</summary>
    Task NotifyProgressedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica bloqueo de la brecha.</summary>
    Task NotifyBlockedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica resolución de la brecha.</summary>
    Task NotifyResolvedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica aceptación formal del riesgo.</summary>
    Task NotifyRiskAcceptedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);

    /// <summary>Notifica cierre definitivo de la brecha.</summary>
    Task NotifyClosedAsync(ComplianceGap gap, Guid actorId, CancellationToken ct = default);
}
