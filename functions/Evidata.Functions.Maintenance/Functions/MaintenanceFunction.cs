using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Maintenance.Functions;

public class MaintenanceFunction
{
    private readonly ILogger<MaintenanceFunction> _logger;
    public MaintenanceFunction(ILogger<MaintenanceFunction> logger) => _logger = logger;

    /// <summary>Timer trigger: ejecuta tareas de mantenimiento cada hora.</summary>
    [Function(nameof(MaintenanceFunction))]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Maintenance: inicio ciclo. IsPastDue={IsPastDue}",
            timerInfo.IsPastDue);

        // TODO Fase 7: limpiar registros expirados, archivar audit logs, purgar outbox Dead
        await Task.Delay(10, ct);

        _logger.LogInformation("Maintenance: ciclo completado");
    }
}
