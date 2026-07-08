using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Application.Abstractions;

/// <summary>
/// Servicio central de auditoría. Todos los módulos lo inyectan para registrar
/// acciones sensibles (creación de tenant, cambio de roles, acceso a documentos, etc.)
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        Guid tenantId,
        Guid? userId,
        string action,
        string resource,
        Guid? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken ct = default);
}
