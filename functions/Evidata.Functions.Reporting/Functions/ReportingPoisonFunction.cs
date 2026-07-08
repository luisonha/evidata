using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Functions;

public class ReportingPoisonFunction
{
    private readonly ILogger<ReportingPoisonFunction> _logger;
    public ReportingPoisonFunction(ILogger<ReportingPoisonFunction> logger) => _logger = logger;

    [Function(nameof(ReportingPoisonFunction))]
    public Task Run(
        [QueueTrigger("report-generation-poison", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        var preview = rawMessage.Length > 200 ? rawMessage[..200] + "..." : rawMessage;
        _logger.LogCritical(
            "🚨 POISON MESSAGE en 'report-generation-poison'. Preview: {Preview}", preview);
        return Task.CompletedTask;
    }
}
