using System.Text;
using System.Text.Json;
using Evidata.Functions.Reporting.Handlers;
using Evidata.Functions.Reporting.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Functions;

public class ReportingFunction
{
    private readonly ILogger<ReportingFunction> _logger;
    private readonly RatReportHandler _ratHandler;
    private readonly GapReportHandler _gapHandler;

    public ReportingFunction(
        ILogger<ReportingFunction> logger,
        RatReportHandler ratHandler,
        GapReportHandler gapHandler)
    {
        _logger = logger;
        _ratHandler = ratHandler;
        _gapHandler = gapHandler;
    }

    [Function(nameof(ReportingFunction))]
    public async Task Run(
        [QueueTrigger("report-generation", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reporting: mensaje recibido");

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

        _logger.LogInformation("Reporting: {MessageType} CorrelationId={CorrelationId}",
            envelope.MessageType, envelope.CorrelationId);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        if (envelope.MessageType != "report.generate.v1")
        {
            _logger.LogWarning("Reporting: tipo {Type} no reconocido — ignorando", envelope.MessageType);
            return;
        }

        var payload = JsonSerializer.Deserialize<ReportJobRequestPayload>(envelope.Payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload is null)
        {
            _logger.LogWarning("Payload ReportJobRequest nulo — descartando");
            return;
        }

        switch (payload.ReportType)
        {
            case "RAT":
                await _ratHandler.HandleAsync(payload.JobId, payload.TenantId, ct);
                break;

            case "Gaps":
                await _gapHandler.HandleAsync(payload.JobId, payload.TenantId, ct);
                break;

            default:
                _logger.LogWarning(
                    "ReportType '{Type}' sin handler registrado — ignorando", payload.ReportType);
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
