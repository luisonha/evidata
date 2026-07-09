using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Mcp.Infrastructure.Interactions;

public sealed class McpInteractionService : IMcpInteractionService
{
    private readonly McpDbContext _db;

    public McpInteractionService(McpDbContext db) => _db = db;

    public async Task<McpInteraction> RecordAsync(
        Guid tenantId, Guid userId, string question, string answer,
        McpRiskLevel riskLevel, bool usedTenantContext, bool requiresHumanReview,
        CancellationToken ct = default)
    {
        var interaction = McpInteraction.Record(tenantId, userId, question, answer,
            riskLevel, usedTenantContext, requiresHumanReview);
        _db.McpInteractions.Add(interaction);
        await _db.SaveChangesAsync(ct);
        return interaction;
    }

    public async Task<McpInteraction> RecordFailedAsync(
        Guid tenantId, Guid userId, string question, CancellationToken ct = default)
    {
        var interaction = McpInteraction.RecordFailed(tenantId, userId, question);
        _db.McpInteractions.Add(interaction);
        await _db.SaveChangesAsync(ct);
        return interaction;
    }

    public async Task<McpCitation> AddCitationAsync(
        Guid interactionId, McpCitationSourceType sourceType, string sourceId,
        string fragment, string? sourceVersion = null, CancellationToken ct = default)
    {
        var interaction = await GetRequiredAsync(interactionId, ct);
        var citation = interaction.AddCitation(sourceType, sourceId, fragment, sourceVersion);
        await _db.SaveChangesAsync(ct);
        return citation;
    }

    public async Task RequestHumanReviewAsync(Guid interactionId, CancellationToken ct = default)
    {
        var interaction = await GetRequiredAsync(interactionId, ct);
        interaction.RequestHumanReview();
        await _db.SaveChangesAsync(ct);
    }

    public Task<McpInteraction?> GetByIdAsync(Guid interactionId, CancellationToken ct = default) =>
        _db.McpInteractions
            .Include(i => i.Citations)
            .FirstOrDefaultAsync(i => i.Id == interactionId, ct);

    public async Task<IReadOnlyList<McpInteraction>> GetRecentByTenantAsync(
        Guid tenantId, int limit = 20, CancellationToken ct = default)
    {
        return await _db.McpInteractions
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<McpInteraction>> GetPendingReviewAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        return await _db.McpInteractions
            .Where(i => i.TenantId == tenantId
                     && i.Status == McpInteractionStatus.ReviewRequested)
            .OrderBy(i => i.OccurredAt)
            .ToListAsync(ct);
    }

    private async Task<McpInteraction> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        return await _db.McpInteractions
                   .Include(i => i.Citations)
                   .FirstOrDefaultAsync(i => i.Id == id, ct)
               ?? throw new KeyNotFoundException($"McpInteraction {id} no encontrada.");
    }
}
