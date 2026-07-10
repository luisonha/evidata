using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Application.Abstractions;

/// <summary>
/// Servicio central de auditoría. Todos los módulos lo inyectan para registrar
/// acciones sensibles (creación de tenant, cambio de roles, acceso a documentos, etc.)
/// 
/// Nuevo contrato P1-009: Soporta campos formales del contrato de AuditEvent:
/// eventType, result, correlationId, metadata (json/dictionary tipado).
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Registrar evento de auditoría con forma completa del contrato P1-009.
    /// </summary>
    Task LogAsync(
        Guid tenantId,
        Guid? userId,
        string eventType,
        string resource,
        Guid? resourceId = null,
        AuditEventResult result = AuditEventResult.Success,
        string? correlationId = null,
        Dictionary<string, object?>? metadata = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken ct = default);

    /// <summary>
    /// Método legacy para compatibilidad. Mapea parámetros antiguos a nueva firma.
    /// </summary>
    [Obsolete("Usar LogAsync con parámetros formales (eventType, result, correlationId, metadata). Este método es solo para compatibilidad.")]
    Task LogLegacyAsync(
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
