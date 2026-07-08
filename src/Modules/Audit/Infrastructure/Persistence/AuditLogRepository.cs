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
}
