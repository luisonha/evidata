using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Audit.Infrastructure.Persistence;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _ctx;
    public AuditLogRepository(AuditDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
    {
        _ctx.AuditLogs.Add(entry);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => await _ctx.AuditLogs
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetByResourceAsync(Guid tenantId, string resource, Guid resourceId, CancellationToken ct = default)
        => await _ctx.AuditLogs
            .Where(x => x.TenantId == tenantId && x.Resource == resource && x.ResourceId == resourceId)
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<AuditLog> Events, int TotalCount)> GetByTenantWithFiltersAsync(
        Guid tenantId,
        string? eventType = null,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _ctx.AuditLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        // Apply eventType filter if provided
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(x => x.EventType == eventType);

        // Apply actorUserId filter if provided
        if (actorUserId.HasValue)
            query = query.Where(x => x.UserId == actorUserId.Value);

        // Apply targetUserId filter if provided (maps to ResourceId in AuditLog)
        if (targetUserId.HasValue)
            query = query.Where(x => x.ResourceId == targetUserId.Value);

        // Apply date range filter if provided
        if (fromDate.HasValue)
            query = query.Where(x => x.OccurredAt >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(x => x.OccurredAt <= toDate.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and ordering
        var events = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (events, totalCount);
    }
}
