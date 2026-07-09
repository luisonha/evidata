using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Application.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid TenantId,
    Guid? UserId,
    string Action,
    string Resource,
    Guid? ResourceId,
    string? Details,
    string? IpAddress,
    DateTime OccurredAt,
    AuditSeverity Severity
);
