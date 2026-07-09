using System.Text;
using System.Text.Json;
using Evidata.Functions.McpBatch.Handlers;
using Evidata.Functions.McpBatch.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.McpBatch.Functions;

/// <summary>
/// Procesa trabajos batch MCP desde la cola <c>mcp-batch</c>.
///
/// MessageTypes soportados:
/// - <c>mcp.retry.v1</c>         → McpRetryHandler
/// - <c>mcp.hitl.escalation.v1</c> → HitlEscalationHandler
/// </summary>
public class McpBatchFunction
{
    private readonly ILogger<McpBatchFunction> _logger;
    private readonly McpRetryHandler _retryHandler;
    private readonly HitlEscalationHandler _escalationHandler;

    public McpBatchFunction(
        ILogger<McpBatchFunction> logger,
        McpRetryHandler retryHandler,
        HitlEscalationHandler escalationHandler)
    {
        _logger = logger;
        _retryHandler = retryHandler;
        _escalationHandler = escalationHandler;
    }

    [Function(nameof(McpBatchFunction))]
    public async Task Run(
        [QueueTrigger("mcp-batch", Connection = "AzureWebJobsStorage")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("McpBatch: mensaje recibido");

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
            _logger.LogError(ex, "Error deserializando mensaje McpBatch");
            throw;
        }

        if (envelope is null)
        {
            _logger.LogWarning("Envelope nulo — descartando");
            return;
        }

        _logger.LogInformation(
            "McpBatch: {MessageType} CorrelationId={CorrelationId}",
            envelope.MessageType, envelope.CorrelationId);

        await DispatchAsync(envelope, cancellationToken);
    }

    private async Task DispatchAsync(QueueMessageEnvelope envelope, CancellationToken ct)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        switch (envelope.MessageType)
        {
            case "mcp.retry.v1":
            {
                var payload = JsonSerializer.Deserialize<McpRetryPayload>(envelope.Payload, opts);
                if (payload is null) { _logger.LogWarning("Payload McpRetry nulo — descartando"); return; }
                var result = await _retryHandler.HandleAsync(payload, ct);
                _logger.LogInformation(
                    "McpBatch retry completado: {Elevated} elevadas, {Skipped} omitidas",
                    result.Elevated, result.Skipped);
                break;
            }
            case "mcp.hitl.escalation.v1":
            {
                var payload = JsonSerializer.Deserialize<McpHitlEscalationPayload>(envelope.Payload, opts);
                if (payload is null) { _logger.LogWarning("Payload HitlEscalation nulo — descartando"); return; }
                var result = await _escalationHandler.HandleAsync(payload, ct);
                _logger.LogInformation(
                    "McpBatch HITL escalation: {Count} tareas estancadas detectadas",
                    result.StaleTasksDetected);
                break;
            }
            default:
                _logger.LogWarning(
                    "McpBatch: tipo {Type} sin handler — ignorando", envelope.MessageType);
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
