namespace Evidata.Worker.Outbox.Messaging;

public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string TenantId { get; init; }
    public required string Destination { get; init; }        // Destino lógico
    public required string MessageType { get; init; }
    public required string Payload { get; init; }           // JSON
    public string? CorrelationId { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public string? ErrorMessage { get; set; }
}

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Dead
}
