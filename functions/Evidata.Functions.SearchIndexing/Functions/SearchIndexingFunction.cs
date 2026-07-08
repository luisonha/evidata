using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.SearchIndexing.Functions;

public class SearchIndexingFunction
{
    private readonly ILogger<SearchIndexingFunction> _logger;
    public SearchIndexingFunction(ILogger<SearchIndexingFunction> logger) => _logger = logger;

    [Function(nameof(SearchIndexingFunction))]
    public async Task Run(
        [QueueTrigger("search-indexing", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        _logger.LogInformation("SearchIndexing: mensaje recibido");

        var json = TryDecodeBase64(rawMessage);
        using var doc = JsonDocument.Parse(json);
        var messageType = doc.RootElement.TryGetProperty("messageType", out var mt) ? mt.GetString() : "unknown";

        _logger.LogInformation("SearchIndexing: procesando {MessageType}", messageType);

        // TODO Fase 3: indexar entidad en Azure Cognitive Search / PostgreSQL FTS
        await Task.Delay(10, ct);

        _logger.LogInformation("SearchIndexing: {MessageType} procesado OK", messageType);
    }

    private static string TryDecodeBase64(string value)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return value; }
    }
}
