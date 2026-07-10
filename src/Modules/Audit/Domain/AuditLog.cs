using System.Text.Json;
using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Audit.Domain;

/// <summary>
/// Evento de auditoría integral que cumple con el contrato P1-009.
/// Shape mínimo: id, tenantId, eventType, resourceType, resourceId, actorUserId, occurredAt, result, correlationId, metadata.
/// </summary>
public class AuditLog : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    
    /// <summary>Usuario actor responsable de la acción. Mapea a actorUserId en contrato.</summary>
    public Guid? UserId { get; private set; }
    
    /// <summary>Tipo de evento auditable. Mapea a eventType en contrato.</summary>
    public string EventType { get; private set; } = default!;
    
    /// <summary>Tipo de recurso auditado. Mapea a resourceType en contrato.</summary>
    public string Resource { get; private set; } = default!;
    
    /// <summary>Identificador del recurso auditado. Mapea a resourceId en contrato.</summary>
    public Guid? ResourceId { get; private set; }
    
    /// <summary>Resultado del evento: Success, Failure, Blocked.</summary>
    public AuditEventResult Result { get; private set; } = AuditEventResult.Success;
    
    /// <summary>Correlation ID para trazabilidad cross-request. Mapea a correlationId en contrato.</summary>
    public string? CorrelationId { get; private set; }
    
    /// <summary>Metadatos estructurados (JSON serializado). Mapea a metadata en contrato.</summary>
    public string? Metadata { get; private set; }
    
    /// <summary>IP del cliente que originó la acción (legacy, mantenido por compatibilidad).</summary>
    public string? IpAddress { get; private set; }
    
    public DateTime OccurredAt { get; private set; }
    
    /// <summary>Nivel de severidad (legacy, mantenido por compatibilidad).</summary>
    public AuditSeverity Severity { get; private set; }

    private AuditLog() { }

    /// <summary>
    /// Crear evento de auditoría con todos los campos formales del contrato.
    /// </summary>
    public static AuditLog Create(
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
        DateTime? occurredAtOverride = null,
        string? metadataJsonRaw = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        string? metadataJson = metadataJsonRaw;
        if (metadataJson == null && metadata != null && metadata.Count > 0)
        {
            metadataJson = JsonSerializer.Serialize(metadata);
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EventType = eventType,
            Resource = resource,
            ResourceId = resourceId,
            Result = result,
            CorrelationId = correlationId,
            Metadata = metadataJson,
            IpAddress = ipAddress,
            Severity = severity,
            OccurredAt = occurredAtOverride ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Método de compatibilidad legacy: mapea Action a EventType.
    /// </summary>
    [Obsolete("Usar Create() con parámetro eventType directo. Este método es solo para compatibilidad.")]
    public static AuditLog CreateLegacy(
        Guid tenantId,
        Guid? userId,
        string action,
        string resource,
        Guid? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        Dictionary<string, object?>? metadata = null;
        if (!string.IsNullOrWhiteSpace(details))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(details) 
                    ?? new Dictionary<string, object?> { { "details", details } };
            }
            catch
            {
                metadata = new Dictionary<string, object?> { { "details", details } };
            }
        }

        return Create(
            tenantId, userId, action, resource, resourceId, 
            AuditEventResult.Success, null, metadata, ipAddress, severity);
    }

    /// <summary>
    /// Deserializar metadatos JSON a diccionario tipado.
    /// Retorna diccionario vacío si Metadata es null o inválido.
    /// </summary>
    public Dictionary<string, object?> GetMetadata()
    {
        if (string.IsNullOrWhiteSpace(Metadata))
            return new Dictionary<string, object?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(Metadata) 
                ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
