using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.SearchIndexing.Functions;

public class SearchIndexingPoisonFunction
{
    private readonly ILogger<SearchIndexingPoisonFunction> _logger;
    public SearchIndexingPoisonFunction(ILogger<SearchIndexingPoisonFunction> logger) => _logger = logger;

    [Function(nameof(SearchIndexingPoisonFunction))]
    public Task Run(
        [QueueTrigger("search-indexing-poison", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        var preview = rawMessage.Length > 200 ? rawMessage[..200] + "..." : rawMessage;
        _logger.LogCritical(
            "🚨 POISON MESSAGE en 'search-indexing-poison'. Preview: {Preview}", preview);
        return Task.CompletedTask;
    }
}
