using Evidata.Functions.McpBatch.Models;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.McpBatch.Handlers;

/// <summary>
/// Detecta tareas HITL en estado Open que llevan más de <see cref="McpHitlEscalationPayload.StaleThresholdMinutes"/>
/// sin ser asignadas y emite un log estructurado de escalación.
///
/// En producción, este log es consumido por Azure Monitor y dispara alertas al equipo de cumplimiento.
/// </summary>
public class HitlEscalationHandler
{
    private readonly McpDbContext _db;
    private readonly ILogger<HitlEscalationHandler> _logger;

    public HitlEscalationHandler(McpDbContext db, ILogger<HitlEscalationHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<HitlEscalationResult> HandleAsync(
        McpHitlEscalationPayload payload, CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow.AddMinutes(-payload.StaleThresholdMinutes);

        var query = _db.McpReviewTasks
            .Where(t => t.Status == McpReviewTaskStatus.Open)
            .Where(t => t.AssignedTo == null)
            .Where(t => t.CreatedAt <= threshold);

        if (payload.TenantId.HasValue)
            query = query.Where(t => t.TenantId == payload.TenantId.Value);

        var stale = await query
            .OrderBy(t => t.CreatedAt)
            .Take(payload.BatchSize)
            .Select(t => new { t.Id, t.TenantId, t.InteractionId, t.CreatedAt })
            .ToListAsync(ct);

        if (stale.Count == 0)
        {
            _logger.LogInformation(
                "HitlEscalation: no hay tareas estancadas (umbral={Minutes}min)",
                payload.StaleThresholdMinutes);
            return new HitlEscalationResult(0);
        }

        foreach (var task in stale)
        {
            var minutesOpen = (int)(DateTimeOffset.UtcNow - task.CreatedAt).TotalMinutes;

            // Log estructurado — Azure Monitor Alert Rule filtra por EventId 9001
            _logger.LogWarning(
                "⚠ HITL_STALE_TASK TaskId={TaskId} TenantId={TenantId} InteractionId={InteractionId} " +
                "MinutesOpen={MinutesOpen} Threshold={Threshold}",
                task.Id, task.TenantId, task.InteractionId, minutesOpen, payload.StaleThresholdMinutes);
        }

        _logger.LogInformation(
            "HitlEscalation: {Count} tareas estancadas detectadas (>{Minutes}min sin asignación)",
            stale.Count, payload.StaleThresholdMinutes);

        return new HitlEscalationResult(stale.Count);
    }
}

public record HitlEscalationResult(int StaleTasksDetected);
