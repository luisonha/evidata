using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.TenantManagement.Infrastructure.Persistence;

public class TenantRepository(EvidataDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default)
        => await db.Tenants.ToListAsync(ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Update(tenant);
        await db.SaveChangesAsync(ct);
    }
}
