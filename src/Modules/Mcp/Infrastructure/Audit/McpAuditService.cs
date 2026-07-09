using Evidata.Modules.Mcp.Application.Audit;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Mcp.Infrastructure.Audit;

/// <summary>
/// Servicio de auditoría MCP — consultas de solo lectura sobre el schema <c>mcp</c>.
/// </summary>
public sealed class McpAuditService : IMcpAuditService
{
    private readonly McpDbContext _db;

    public McpAuditService(McpDbContext db)
    {
        _db = db;
    }

    // ── Historial ─────────────────────────────────────────────────────────────

    public async Task<McpInteractionHistory> GetInteractionHistoryAsync(
        McpAuditQuery query, CancellationToken ct = default)
    {
        var q = _db.McpInteractions
            .AsNoTracking()
            .Where(i => i.TenantId == query.TenantId);

        if (query.From.HasValue)
            q = q.Where(i => i.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(i => i.OccurredAt <= query.To.Value);

        if (query.RiskLevel.HasValue)
            q = q.Where(i => i.RiskLevel == query.RiskLevel.Value);

        if (query.Status.HasValue)
            q = q.Where(i => i.Status == query.Status.Value);

        var total = await q.CountAsync(ct);

        var skip = (query.Page - 1) * query.PageSize;

        var items = await q
            .OrderByDescending(i => i.OccurredAt)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(i => new McpInteractionEntry(
                i.Id,
                i.UserId,
                i.Question,
                i.RiskLevel,
                i.Status,
                i.UsedTenantContext,
                i.RequiresHumanReview,
                i.Citations.Count,
                i.OccurredAt))
            .ToListAsync(ct);

        return new McpInteractionHistory(items, total, query.Page, query.PageSize);
    }

    // ── Métricas de feedback ──────────────────────────────────────────────────

    public async Task<McpFeedbackMetrics> GetFeedbackMetricsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        // Join feedback ← interaction para obtener nivel de riesgo
        var data = await (
            from f in _db.McpFeedbacks.AsNoTracking()
            join i in _db.McpInteractions.AsNoTracking()
                on f.InteractionId equals i.Id
            where f.TenantId == tenantId
            select new { f.Rating, i.RiskLevel }
        ).ToListAsync(ct);

        var total = data.Count;
        var helpful = data.Count(x => x.Rating == McpFeedbackRating.Helpful);
        var notHelpful = total - helpful;

        var helpfulByRisk = data
            .Where(x => x.Rating == McpFeedbackRating.Helpful)
            .GroupBy(x => x.RiskLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        var notHelpfulByRisk = data
            .Where(x => x.Rating == McpFeedbackRating.NotHelpful)
            .GroupBy(x => x.RiskLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        var percent = total > 0 ? Math.Round((double)helpful / total * 100, 2) : 0.0;

        return new McpFeedbackMetrics(
            TenantId: tenantId,
            TotalFeedbacks: total,
            HelpfulCount: helpful,
            NotHelpfulCount: notHelpful,
            HelpfulPercent: percent,
            HelpfulByRiskLevel: helpfulByRisk,
            NotHelpfulByRiskLevel: notHelpfulByRisk);
    }

    // ── Resumen HITL ──────────────────────────────────────────────────────────

    public async Task<McpHitlSummary> GetHitlSummaryAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var counts = await _db.McpReviewTasks
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(McpReviewTaskStatus s) =>
            counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        return new McpHitlSummary(
            TenantId: tenantId,
            OpenCount: Get(McpReviewTaskStatus.Open),
            InProgressCount: Get(McpReviewTaskStatus.InProgress),
            ApprovedCount: Get(McpReviewTaskStatus.Approved),
            RejectedCount: Get(McpReviewTaskStatus.Rejected));
    }
}
