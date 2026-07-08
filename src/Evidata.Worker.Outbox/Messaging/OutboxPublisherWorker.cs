using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Evidata.Worker.Outbox.Messaging;

/// <summary>
/// Background service que lee OutboxMessages pendientes y los publica a las colas.
/// En Fase 1 esto leerá de PostgreSQL via EF Core.
/// Por ahora es el skeleton con la lógica de polling.
/// </summary>
public class OutboxPublisherWorker : BackgroundService
{
    private readonly IMessagePublisher _publisher;
    private readonly IDestinationResolver _destinationResolver;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public OutboxPublisherWorker(
        IMessagePublisher publisher,
        IDestinationResolver destinationResolver,
        ILogger<OutboxPublisherWorker> logger)
    {
        _publisher = publisher;
        _destinationResolver = destinationResolver;
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

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        // TODO Fase 1: leer OutboxMessages pendientes de PostgreSQL via IOutboxRepository
        // Por ahora: placeholder que confirma el worker está corriendo
        _logger.LogDebug("OutboxPublisher: ciclo de polling (repositorio pendiente Fase 1)");
        await Task.CompletedTask;
    }
}
