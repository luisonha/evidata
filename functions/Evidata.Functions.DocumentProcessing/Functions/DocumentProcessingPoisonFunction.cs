using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.DocumentProcessing.Functions;

/// <summary>
/// Consume la queue de poison messages de document-processing.
/// Azure Storage Queues mueve automáticamente mensajes a {queue}-poison
/// tras 5 intentos fallidos de dequeue.
/// Esta función registra la alerta y descarta el mensaje para evitar acumulación.
/// </summary>
public class DocumentProcessingPoisonFunction
{
    private readonly ILogger<DocumentProcessingPoisonFunction> _logger;

    public DocumentProcessingPoisonFunction(ILogger<DocumentProcessingPoisonFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(DocumentProcessingPoisonFunction))]
    public Task Run(
        [QueueTrigger("document-processing-poison", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        var preview = rawMessage.Length > 200 ? rawMessage[..200] + "..." : rawMessage;

        _logger.LogCritical(
            "🚨 POISON MESSAGE detectado en 'document-processing'. " +
            "Mensaje descartado tras 5 intentos. Preview: {Preview}",
            preview);

        // Intentar extraer metadata para diagnóstico
        TryLogMetadata(rawMessage);

        // El mensaje se elimina automáticamente al completar sin excepción
        // Si lanzamos excepción, Azure lo reencola en la poison queue indefinidamente
        return Task.CompletedTask;
    }

    private void TryLogMetadata(string rawMessage)
    {
        try
        {
            var json = IsBase64(rawMessage)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(rawMessage))
                : rawMessage;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var messageType = root.TryGetProperty("messageType", out var mt) ? mt.GetString() : "unknown";
            var correlationId = root.TryGetProperty("correlationId", out var ci) ? ci.GetString() : "unknown";
            var sentAt = root.TryGetProperty("sentAt", out var sa) ? sa.GetString() : "unknown";

            _logger.LogCritical(
                "Poison metadata — MessageType={MessageType} CorrelationId={CorrelationId} SentAt={SentAt}",
                messageType, correlationId, sentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo parsear metadata del poison message");
        }
    }

    private static bool IsBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0) return false;
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}
