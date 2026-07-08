using System.Text.Json.Serialization;

namespace Evidata.Functions.DocumentProcessing.Models;

/// <summary>
/// Envelope estándar que todos los mensajes del Outbox envían a las colas.
/// Corresponde al envelope que construye AzureStorageQueuePublisher.
/// </summary>
public record QueueMessageEnvelope(
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("sentAt")] DateTimeOffset SentAt,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion
);
