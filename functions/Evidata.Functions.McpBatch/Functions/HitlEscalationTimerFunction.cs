using Evidata.Functions.McpBatch.Handlers;
using Evidata.Functions.McpBatch.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.McpBatch.Functions;

/// <summary>
/// Timer que escanea periódicamente tareas HITL estancadas (sin asignación > 2h).
/// Se ejecuta cada hora.
/// </summary>
public class HitlEscalationTimerFunction
{
    private readonly ILogger<HitlEscalationTimerFunction> _logger;
    private readonly HitlEscalationHandler _handler;

    public HitlEscalationTimerFunction(
        ILogger<HitlEscalationTimerFunction> logger,
        HitlEscalationHandler handler)
    {
        _logger = logger;
        _handler = handler;
    }

    [Function(nameof(HitlEscalationTimerFunction))]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "HitlEscalationTimer: inicio. IsPastDue={IsPastDue}",
            timer.IsPastDue);

        var payload = new McpHitlEscalationPayload(
            TenantId: null,
            StaleThresholdMinutes: 120,
            BatchSize: 100);

        var result = await _handler.HandleAsync(payload, cancellationToken);

        _logger.LogInformation(
            "HitlEscalationTimer: completado — {Count} tareas estancadas detectadas",
            result.StaleTasksDetected);
    }
}
