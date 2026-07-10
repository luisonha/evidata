using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Application.DTOs;

/// <summary>
/// DTO completo de AuditLog que expone el shape del contrato P1-009.
/// Incluye campos formales: eventType, result, correlationId, metadata.
/// </summary>
public record AuditLogDto(
    Guid Id,
    Guid TenantId,
    Guid? UserId,
    string EventType,
    string Resource,
    Guid? ResourceId,
    AuditEventResult Result,
    string? CorrelationId,
    string? Metadata,
    string? IpAddress,
    DateTime OccurredAt,
    AuditSeverity Severity
);
