using System.Text;
using System.Text.Json;
using Evidata.Functions.SearchIndexing.Handlers;
using Evidata.Functions.SearchIndexing.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.SearchIndexing.Functions;

public class SearchIndexingFunction
{
    private readonly ILogger<SearchIndexingFunction> _logger;
    private readonly DocumentIndexingHandler _documentHandler;
    private readonly RatIndexingHandler _ratHandler;

    public SearchIndexingFunction(
        ILogger<SearchIndexingFunction> logger,
        DocumentIndexingHandler documentHandler,
        RatIndexingHandler ratHandler)
    {
        _logger = logger;
        _documentHandler = documentHandler;
        _ratHandler = ratHandler;
    }

    [Function(nameof(SearchIndexingFunction))]
    public async Task Run(
        [QueueTrigger("search-indexing", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SearchIndexing: mensaje recibido");

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
            _logger.LogWarning("Envelope nulo — descartando");
            return;
        }

        _logger.LogInformation("SearchIndexing: {MessageType} CorrelationId={CorrelationId}",
            envelope.MessageType, envelope.CorrelationId);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        switch (envelope.MessageType)
        {
            case "document.indexed.v1":
            {
                var payload = JsonSerializer.Deserialize<DocumentIndexPayload>(envelope.Payload, opts);
                if (payload is null) { _logger.LogWarning("Payload DocumentIndex nulo — descartando"); return; }
                await _documentHandler.HandleAsync(payload, ct);
                break;
            }
            case "rat.indexed.v1":
            {
                var payload = JsonSerializer.Deserialize<RatIndexPayload>(envelope.Payload, opts);
                if (payload is null) { _logger.LogWarning("Payload RatIndex nulo — descartando"); return; }
                await _ratHandler.HandleAsync(payload, ct);
                break;
            }
            default:
                _logger.LogWarning("SearchIndexing: tipo {Type} sin handler — ignorando", envelope.MessageType);
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
