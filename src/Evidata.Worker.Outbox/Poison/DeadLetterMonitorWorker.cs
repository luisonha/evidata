using Evidata.Worker.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Evidata.Worker.Outbox.Poison;

/// <summary>
/// Monitorea mensajes en estado Dead en la tabla outbox_messages.
/// Se ejecuta cada 5 minutos y emite alertas críticas si hay acumulación.
/// Threshold: alerta si > 10 mensajes Dead en los últimos 30 min.
/// </summary>
public class DeadLetterMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadLetterMonitorWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private const int AlertThreshold = 10;

    public DeadLetterMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<DeadLetterMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeadLetterMonitor iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadLettersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en ciclo DeadLetterMonitor");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckDeadLettersAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        var since = DateTimeOffset.UtcNow.AddMinutes(-30);
        var deadCount = await ctx.OutboxMessages
            .Where(m => m.Status == Messaging.OutboxMessageStatus.Dead && m.ProcessedAt >= since)
            .CountAsync(ct);

        if (deadCount >= AlertThreshold)
        {
            _logger.LogCritical(
                "🚨 ALERTA DEAD LETTER: {Count} mensajes fallidos en los últimos 30 min. " +
                "Revisar outbox_messages WHERE status='Dead'. Posible problema de conectividad con Storage Queues.",
                deadCount);
        }
        else if (deadCount > 0)
        {
            _logger.LogWarning(
                "DeadLetterMonitor: {Count} mensajes Dead en los últimos 30 min (umbral: {Threshold})",
                deadCount, AlertThreshold);
        }
        else
        {
            _logger.LogDebug("DeadLetterMonitor: sin mensajes Dead recientes ✓");
        }
    }
}
