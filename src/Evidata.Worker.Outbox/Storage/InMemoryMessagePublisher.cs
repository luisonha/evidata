using Microsoft.Extensions.Logging;

namespace Evidata.Worker.Outbox.Storage;

/// <summary>
/// Implementación in-memory para tests y desarrollo local sin colas reales.
/// </summary>
public class InMemoryMessagePublisher : Evidata.Worker.Outbox.Messaging.IMessagePublisher
{
    private readonly ILogger<InMemoryMessagePublisher> _logger;
    public readonly List<(string Destination, string MessageType, string Payload)> Published = new();

    public InMemoryMessagePublisher(ILogger<InMemoryMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string destination, string messageType, string payload,
        string correlationId, CancellationToken cancellationToken = default)
    {
        Published.Add((destination, messageType, payload));
        _logger.LogDebug("[InMemory] Publicado → {Destination} | {MessageType}", destination, messageType);
        return Task.CompletedTask;
    }
}
