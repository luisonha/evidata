using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Functions;

public class ReportingFunction
{
    private readonly ILogger<ReportingFunction> _logger;
    public ReportingFunction(ILogger<ReportingFunction> logger) => _logger = logger;

    [Function(nameof(ReportingFunction))]
    public async Task Run(
        [QueueTrigger("report-generation", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken ct)
    {
        _logger.LogInformation("Reporting: mensaje recibido");

        var json = TryDecodeBase64(rawMessage);
        using var doc = JsonDocument.Parse(json);
        var messageType = doc.RootElement.TryGetProperty("messageType", out var mt) ? mt.GetString() : "unknown";

        _logger.LogInformation("Reporting: generando reporte {MessageType}", messageType);

        // TODO Fase 6: generación de reportes PDF / Excel
        await Task.Delay(10, ct);

        _logger.LogInformation("Reporting: {MessageType} procesado OK", messageType);
    }

    private static string TryDecodeBase64(string value)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return value; }
    }
}
