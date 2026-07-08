using Evidata.Worker.Outbox.Messaging;
using Evidata.Worker.Outbox.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Evidata.Worker.Outbox.Messaging;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en ciclo OutboxPublisher");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var messages = await repository.GetPendingAsync(BatchSize, ct);
        if (messages.Count == 0) return;

        _logger.LogDebug("OutboxPublisher: procesando {Count} mensajes pendientes", messages.Count);

        foreach (var msg in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    msg.Destination,
                    msg.MessageType,
                    msg.Payload,
                    msg.CorrelationId ?? msg.Id.ToString(),
                    ct);

                await repository.MarkSentAsync(msg.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error publicando mensaje {MessageId} (intento {Retry})",
                    msg.Id, msg.RetryCount + 1);

                if (msg.RetryCount + 1 >= 5)
                    await repository.MarkFailedAsync(msg.Id, ex.Message, ct);
                else
                    await repository.IncrementRetryAsync(msg.Id, ct);
            }
        }
    }
}
