using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Infrastructure;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;

    public AuditService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Registrar evento de auditoría completo con todos los campos del contrato.
    /// </summary>
    public async Task LogAsync(
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
        CancellationToken ct = default)
    {
        var entry = AuditLog.Create(
            tenantId, userId, eventType, resource, resourceId,
            result, correlationId, metadata, ipAddress, severity);
        await _repository.AddAsync(entry, ct);
    }

    /// <summary>
    /// Método legacy para compatibilidad. Mapea parámetros antiguos a nueva firma.
    /// </summary>
    [Obsolete("Usar LogAsync con parámetros formales (eventType, result, correlationId, metadata). Este método es solo para compatibilidad.")]
    public async Task LogLegacyAsync(
        Guid tenantId,
        Guid? userId,
        string action,
        string resource,
        Guid? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken ct = default)
    {
        Dictionary<string, object?>? metadata = null;
        if (!string.IsNullOrWhiteSpace(details))
        {
            try
            {
                metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(details) 
                    ?? new Dictionary<string, object?> { { "details", details } };
            }
            catch
            {
                metadata = new Dictionary<string, object?> { { "details", details } };
            }
        }

        await LogAsync(tenantId, userId, action, resource, resourceId, 
            AuditEventResult.Success, null, metadata, ipAddress, severity, ct);
    }
}
