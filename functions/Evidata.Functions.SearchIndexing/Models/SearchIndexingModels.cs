using System.Text.Json.Serialization;

namespace Evidata.Functions.SearchIndexing.Models;

/// <summary>Envelope estándar del Outbox Worker para la cola search-indexing.</summary>
public record QueueMessageEnvelope(
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("sentAt")] DateTimeOffset SentAt,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion
);

/// <summary>Payload para indexación de un documento.</summary>
public record DocumentIndexPayload(
    [property: JsonPropertyName("documentId")] Guid DocumentId,
    [property: JsonPropertyName("tenantId")] Guid TenantId,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("blobPath")] string? BlobPath
);

/// <summary>Payload para indexación de un tratamiento RAT.</summary>
public record RatIndexPayload(
    [property: JsonPropertyName("activityId")] Guid ActivityId,
    [property: JsonPropertyName("tenantId")] Guid TenantId
);
