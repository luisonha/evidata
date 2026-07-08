using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Notifications.Functions;

public class NotificationsFunction
{
    private readonly ILogger<NotificationsFunction> _logger;
    public NotificationsFunction(ILogger<NotificationsFunction> logger) => _logger = logger;

    [Function(nameof(NotificationsFunction))]
    public async Task Run(
        [QueueTrigger("notification-delivery", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        _logger.LogInformation("Notifications: mensaje recibido");

        var json = TryDecodeBase64(rawMessage);
        using var doc = JsonDocument.Parse(json);
        var messageType = doc.RootElement.TryGetProperty("messageType", out var mt) ? mt.GetString() : "unknown";

        _logger.LogInformation("Notifications: enviando {MessageType}", messageType);

        // TODO Fase 5: envío real por email/push via SendGrid / Azure Communication Services
        await Task.Delay(10, ct);

        _logger.LogInformation("Notifications: {MessageType} procesado OK", messageType);
    }

    private static string TryDecodeBase64(string value)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return value; }
    }
}
