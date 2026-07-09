using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Notifications.Functions;

public class NotificationsPoisonFunction
{
    private readonly ILogger<NotificationsPoisonFunction> _logger;
    public NotificationsPoisonFunction(ILogger<NotificationsPoisonFunction> logger) => _logger = logger;

    [Function(nameof(NotificationsPoisonFunction))]
    public Task Run(
        [QueueTrigger("notification-delivery-poison", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        var preview = rawMessage.Length > 200 ? rawMessage[..200] + "..." : rawMessage;
        _logger.LogCritical(
            "🚨 POISON MESSAGE en 'notification-delivery-poison'. Preview: {Preview}", preview);
        return Task.CompletedTask;
    }
}
