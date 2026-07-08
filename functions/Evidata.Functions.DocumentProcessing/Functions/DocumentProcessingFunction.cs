using System.Text;
using System.Text.Json;
using Evidata.Functions.DocumentProcessing.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.DocumentProcessing.Functions;

public class DocumentProcessingFunction
{
    private readonly ILogger<DocumentProcessingFunction> _logger;

    public DocumentProcessingFunction(ILogger<DocumentProcessingFunction> logger)
    {
        _logger = logger;
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
            // Los mensajes del Outbox llegan en Base64
            var json = IsBase64(rawMessage)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(rawMessage))
                : rawMessage;

            envelope = JsonSerializer.Deserialize<QueueMessageEnvelope>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando mensaje de queue. Raw: {Raw}", rawMessage);
            throw; // Poison handling: Azure Functions reencola y eventualmente mueve a poison queue
        }

        if (envelope is null)
        {
            _logger.LogWarning("Mensaje nulo tras deserialización — descartando");
            return;
        }

        _logger.LogInformation(
            "DocumentProcessing: procesando {MessageType} CorrelationId={CorrelationId} SchemaVersion={Schema}",
            envelope.MessageType, envelope.CorrelationId, envelope.SchemaVersion);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        switch (envelope.MessageType)
        {
            case "DocumentUploaded":
                await HandleDocumentUploadedAsync(envelope, ct);
                break;
            default:
                _logger.LogWarning("DocumentProcessing: tipo de mensaje desconocido {Type} — ignorando", envelope.MessageType);
                break;
        }
    }

    private async Task HandleDocumentUploadedAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        DocumentUploadedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DocumentUploadedPayload>(envelope.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando payload DocumentUploaded");
            throw;
        }

        if (payload is null)
        {
            _logger.LogWarning("Payload DocumentUploaded nulo — descartando");
            return;
        }

        _logger.LogInformation(
            "DocumentProcessing: procesando documento {DocumentId} tenant={TenantId} archivo={FileName}",
            payload.DocumentId, payload.TenantId, payload.FileName);

        // TODO Fase 3: extracción de metadatos, OCR, indexación
        await Task.Delay(10, ct); // placeholder para work real

        _logger.LogInformation("DocumentProcessing: documento {DocumentId} procesado OK", payload.DocumentId);
    }

    private static bool IsBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0) return false;
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}
