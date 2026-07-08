using System.Text;
using System.Text.Json;
using Evidata.Functions.DocumentProcessing.Handlers;
using Evidata.Functions.DocumentProcessing.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.DocumentProcessing.Functions;

public class DocumentProcessingFunction
{
    private readonly ILogger<DocumentProcessingFunction> _logger;
    private readonly DocumentUploadedHandler _documentUploadedHandler;

    public DocumentProcessingFunction(
        ILogger<DocumentProcessingFunction> logger,
        DocumentUploadedHandler documentUploadedHandler)
    {
        _logger = logger;
        _documentUploadedHandler = documentUploadedHandler;
    }

    /// <summary>
    /// Consume mensajes de la queue 'document-processing' publicados por el Outbox.
    /// Los mensajes llegan en Base64 — el trigger los decodifica automáticamente con isBase64Encoded.
    /// </summary>
    [Function(nameof(DocumentProcessingFunction))]
    public async Task Run(
        [QueueTrigger("document-processing", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("DocumentProcessing: mensaje recibido");

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
            _logger.LogError(ex, "Error deserializando mensaje de queue. Raw: {Raw}", rawMessage);
            throw;
        }

        if (envelope is null)
        {
            _logger.LogWarning("Mensaje nulo tras deserialización — descartando");
            return;
        }

        _logger.LogInformation(
            "DocumentProcessing: {MessageType} CorrelationId={CorrelationId}",
            envelope.MessageType, envelope.CorrelationId);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        switch (envelope.MessageType)
        {
            case "DocumentUploaded":
                var payload = JsonSerializer.Deserialize<DocumentUploadedPayload>(envelope.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (payload is null)
                {
                    _logger.LogWarning("Payload DocumentUploaded nulo — descartando");
                    return;
                }
                await _documentUploadedHandler.HandleAsync(payload, ct);
                break;

            default:
                _logger.LogWarning("Tipo de mensaje desconocido: {Type} — ignorando", envelope.MessageType);
                break;
        }
    }

    private static bool IsBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0) return false;
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}
