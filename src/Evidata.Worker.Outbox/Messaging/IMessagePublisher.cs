namespace Evidata.Worker.Outbox.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(string destination, string messageType, string payload,
        string correlationId, CancellationToken cancellationToken = default);
}
