using System.Text;
using System.Text.Json;
using Evidata.Functions.Notifications.Handlers;
using Evidata.Functions.Notifications.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Notifications.Functions;

public class NotificationsFunction
{
    private readonly ILogger<NotificationsFunction> _logger;
    private readonly GapNotificationHandler _gapHandler;

    public NotificationsFunction(
        ILogger<NotificationsFunction> logger,
        GapNotificationHandler gapHandler)
    {
        _logger = logger;
        _gapHandler = gapHandler;
    }

    /// <summary>
    /// Consume mensajes de la cola 'notification-delivery' publicados por el Outbox Worker.
    /// Los mensajes llegan en Base64 — se decodifican antes de deserializar.
    /// </summary>
    [Function(nameof(NotificationsFunction))]
    public async Task Run(
        [QueueTrigger("notification-delivery", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notifications: mensaje recibido");

        QueueMessageEnvelope? envelope;
        try
        {
            var json = IsBase64(rawMessage)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(rawMessage))
                : rawMessage;

            envelope = JsonSerializer.Deserialize<QueueMessageEnvelope>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando mensaje. Raw={Raw}",
                rawMessage.Length > 200 ? rawMessage[..200] + "..." : rawMessage);
            throw;
        }

        if (envelope is null)
        {
            _logger.LogWarning("Envelope nulo tras deserialización — descartando");
            return;
        }

        _logger.LogInformation(
            "Notifications: {MessageType} CorrelationId={CorrelationId}",
            envelope.MessageType, envelope.CorrelationId);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        // Mensajes de brechas de cumplimiento (gap.*)
        if (envelope.MessageType.StartsWith("gap.", StringComparison.OrdinalIgnoreCase))
        {
            var handled = await _gapHandler.HandleAsync(envelope.MessageType, envelope.Payload, ct);
            if (!handled)
                _logger.LogWarning("Notifications: tipo {Type} no manejado", envelope.MessageType);
            return;
        }

        _logger.LogWarning("Notifications: tipo {Type} sin handler registrado — ignorando",
            envelope.MessageType);
    }

    private static bool IsBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0) return false;
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}
