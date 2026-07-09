using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.TenantManagement.Infrastructure.Persistence;

public class EvidataDbContextFactory : IDesignTimeDbContextFactory<EvidataDbContext>
{
    public EvidataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EvidataDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=evidata;Username=postgres;Password=postgres");
        return new EvidataDbContext(optionsBuilder.Options);
    }
}
