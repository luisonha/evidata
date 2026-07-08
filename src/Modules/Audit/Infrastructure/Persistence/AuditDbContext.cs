using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Audit.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditLogConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
