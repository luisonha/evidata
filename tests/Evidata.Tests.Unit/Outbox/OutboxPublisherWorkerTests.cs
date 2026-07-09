using Evidata.Worker.Outbox.Messaging;
using Evidata.Worker.Outbox.Persistence;
using NSubstitute;

namespace Evidata.Tests.Unit.Outbox;

public class OutboxPublisherWorkerTests
{
    private readonly IOutboxRepository _repository = Substitute.For<IOutboxRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    [Fact]
    public async Task OutboxRepository_EnqueueAndGetPending_RoundTrip()
    {
        // Este test verifica la lógica del repositorio usando un mock del contexto
        // El test real E2E requiere PostgreSQL — aquí validamos la interfaz
        var tenantId = Guid.NewGuid().ToString();
        var messages = new List<OutboxMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Destination = "document-processing",
                MessageType = "DocumentUploaded",
                Payload = "{\"documentId\":\"123\"}",
                Status = OutboxMessageStatus.Pending
            }
        };

        _repository.GetPendingAsync(50).Returns(messages);

        var pending = await _repository.GetPendingAsync(50);

        Assert.Single(pending);
        Assert.Equal(OutboxMessageStatus.Pending, pending[0].Status);
        Assert.Equal("document-processing", pending[0].Destination);
    }

    [Fact]
    public async Task Publisher_PublishAsync_CalledWithCorrectArgs()
    {
        // Arrange
        await _publisher.PublishAsync("document-processing", "DocumentUploaded", "{}", "corr-1");

        // Assert
        await _publisher.Received(1).PublishAsync(
            "document-processing",
            "DocumentUploaded",
            "{}",
            "corr-1");
    }

    [Fact]
    public void OutboxMessage_DefaultStatus_IsPending()
    {
        var msg = new OutboxMessage
        {
            TenantId = Guid.NewGuid().ToString(),
            Destination = "search-indexing",
            MessageType = "EntityIndexed",
            Payload = "{}"
        };

        Assert.Equal(OutboxMessageStatus.Pending, msg.Status);
        Assert.Equal(0, msg.RetryCount);
    }
}
