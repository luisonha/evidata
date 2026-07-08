using System.Text.Json.Serialization;

namespace Evidata.Functions.Notifications.Models;

/// <summary>
/// Envelope estándar que el Outbox Worker envía a la cola notification-delivery.
/// Mismo contrato que AzureStorageQueuePublisher.
/// </summary>
public record QueueMessageEnvelope(
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("sentAt")] DateTimeOffset SentAt,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion
);
