using Azure.Storage.Queues;
using Evidata.Worker.Outbox.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Evidata.Worker.Outbox.Storage;

public class AzureStorageQueuePublisher : IMessagePublisher
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly IDestinationResolver _destinationResolver;
    private readonly ILogger<AzureStorageQueuePublisher> _logger;

    public AzureStorageQueuePublisher(
        IConfiguration configuration,
        IDestinationResolver destinationResolver,
        ILogger<AzureStorageQueuePublisher> logger)
    {
        var connStr = configuration.GetConnectionString("blob")
            ?? configuration.GetConnectionString("azurite")
            ?? throw new InvalidOperationException("Storage connection string not found.");

        _queueServiceClient = new QueueServiceClient(connStr);
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
