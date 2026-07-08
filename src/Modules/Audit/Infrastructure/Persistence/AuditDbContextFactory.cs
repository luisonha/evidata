using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Audit.Infrastructure.Persistence;

public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata",
            b => b.MigrationsAssembly(typeof(AuditDbContextFactory).Assembly.FullName));
        return new AuditDbContext(optionsBuilder.Options);
    }
}
