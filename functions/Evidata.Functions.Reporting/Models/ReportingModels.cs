using System.Text.Json.Serialization;

namespace Evidata.Functions.Reporting.Models;

/// <summary>Envelope estándar del Outbox Worker para la cola report-generation.</summary>
public record QueueMessageEnvelope(
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("sentAt")] DateTimeOffset SentAt,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion
);

/// <summary>
/// Payload del mensaje de solicitud de generación de reporte.
/// Publicado al solicitar un <see cref="Evidata.Modules.Reporting.Domain.ReportJob"/>.
/// </summary>
public record ReportJobRequestPayload(
    [property: JsonPropertyName("jobId")] Guid JobId,
    [property: JsonPropertyName("tenantId")] Guid TenantId,
    [property: JsonPropertyName("reportType")] string ReportType,
    [property: JsonPropertyName("parameters")] string Parameters,
    [property: JsonPropertyName("requestedBy")] Guid RequestedBy
);
