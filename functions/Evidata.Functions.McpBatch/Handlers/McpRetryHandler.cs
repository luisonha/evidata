using Evidata.Functions.McpBatch.Models;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.McpBatch.Handlers;

/// <summary>
/// Crea tareas HITL (<see cref="McpReviewTask"/>) para interacciones MCP fallidas
/// que todavía no tienen una tarea de revisión asignada.
///
/// Las interacciones <see cref="McpInteractionStatus.Failed"/> no pueden cambiar de estado
/// directamente — la acción correcta es crear una tarea de revisión humana para que
/// el equipo de cumplimiento decida cómo proceder.
///
/// Es idempotente: si ya existe un <see cref="McpReviewTask"/> para la interacción, la omite.
/// </summary>
public class McpRetryHandler
{
    private readonly McpDbContext _db;
    private readonly ILogger<McpRetryHandler> _logger;

    public McpRetryHandler(McpDbContext db, ILogger<McpRetryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<McpRetryResult> HandleAsync(McpRetryPayload payload, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-payload.LookbackMinutes);

        // Interacciones fallidas sin tarea de revisión existente
        var failedWithoutTask = await _db.McpInteractions
            .Where(i => i.TenantId == payload.TenantId)
            .Where(i => i.Status == McpInteractionStatus.Failed)
            .Where(i => i.OccurredAt >= cutoff)
            .Where(i => !_db.McpReviewTasks.Any(t => t.InteractionId == i.Id))
            .OrderBy(i => i.OccurredAt)
            .Take(payload.BatchSize)
            .Select(i => new { i.Id, i.TenantId })
            .ToListAsync(ct);

        if (failedWithoutTask.Count == 0)
        {
            _logger.LogInformation(
                "McpRetry: no hay interacciones fallidas sin tarea en los últimos {Minutes}min para tenant {TenantId}",
                payload.LookbackMinutes, payload.TenantId);
            return new McpRetryResult(0, 0);
        }

        int created = 0;

        foreach (var interaction in failedWithoutTask)
        {
            var reviewTask = McpReviewTask.Create(interaction.Id, interaction.TenantId);
            _db.McpReviewTasks.Add(reviewTask);
            created++;

            _logger.LogInformation(
                "McpRetry: tarea HITL creada para interacción fallida {InteractionId} tenant={TenantId}",
                interaction.Id, interaction.TenantId);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "McpRetry: tenant {TenantId} — {Created} tareas HITL creadas",
            payload.TenantId, created);

        return new McpRetryResult(created, Skipped: 0);
    }
}

public record McpRetryResult(int Elevated, int Skipped);

