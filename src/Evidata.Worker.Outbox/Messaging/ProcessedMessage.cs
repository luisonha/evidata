namespace Evidata.Worker.Outbox.Messaging;

/// <summary>
/// Registro de idempotencia. Evita reprocesar mensajes ya consumidos.
/// </summary>
public class ProcessedMessage
{
    public required string MessageId { get; init; }
    public required string MessageType { get; init; }
    public required string ConsumerName { get; init; }
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
