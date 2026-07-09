using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.TenantManagement.Infrastructure.Persistence;

public class EvidataDbContext : DbContext
{
    public EvidataDbContext(DbContextOptions<EvidataDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
