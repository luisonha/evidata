using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Evidata.Worker.Outbox.Storage;

public class AzureStorageQueuePublisher : Evidata.Worker.Outbox.Messaging.IMessagePublisher
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly Evidata.Worker.Outbox.Messaging.IDestinationResolver _destinationResolver;
    private readonly ILogger<AzureStorageQueuePublisher> _logger;

    public AzureStorageQueuePublisher(
        QueueServiceClient queueServiceClient,
        Evidata.Worker.Outbox.Messaging.IDestinationResolver destinationResolver,
        ILogger<AzureStorageQueuePublisher> logger)
    {
        _queueServiceClient = queueServiceClient;
        _destinationResolver = destinationResolver;
        _logger = logger;
    }

    public async Task PublishAsync(string destination, string messageType, string payload,
        string correlationId, CancellationToken cancellationToken = default)
    {
        var queueName = _destinationResolver.Resolve(destination);
        var queueClient = _queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var envelope = new
        {
            MessageType = messageType,
            Payload = payload,
            CorrelationId = correlationId,
            SentAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };

        var message = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope)));

        await queueClient.SendMessageAsync(message, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Mensaje publicado. Destination={Destination} Queue={Queue} Type={MessageType} CorrelationId={CorrelationId}",
            destination, queueName, messageType, correlationId);
    }
}
