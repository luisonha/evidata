namespace Evidata.Functions.McpBatch.Models;

/// <summary>Envelope estándar de mensajes en la cola mcp-batch.</summary>
public record QueueMessageEnvelope(
    string MessageType,
    string Payload,
    string? CorrelationId,
    DateTimeOffset SentAt,
    int SchemaVersion = 1);

/// <summary>
/// Payload para <c>mcp.retry.v1</c>.
/// Solicita reintentar interacciones fallidas de un tenant en la ventana indicada.
/// </summary>
public record McpRetryPayload(
    Guid TenantId,
    /// <summary>Minutos hacia atrás desde ahora para buscar interacciones fallidas.</summary>
    int LookbackMinutes = 60,
    /// <summary>Máximo de interacciones a reintentar en este lote.</summary>
    int BatchSize = 20);

/// <summary>
/// Payload para <c>mcp.hitl.escalation.v1</c>.
/// Solicita escalar tareas HITL abiertas sin asignación después del umbral indicado.
/// </summary>
public record McpHitlEscalationPayload(
    Guid? TenantId,
    /// <summary>Minutos sin asignación para considerar una tarea como estancada.</summary>
    int StaleThresholdMinutes = 120,
    int BatchSize = 50);
