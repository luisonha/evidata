using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Evidence.Infrastructure.Persistence;

public class EvidenceDbContextFactory : IDesignTimeDbContextFactory<EvidenceDbContext>
{
    public EvidenceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EvidenceDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata_dev",
                b => b.MigrationsAssembly(typeof(EvidenceDbContextFactory).Assembly.FullName))
            .Options;

        return new EvidenceDbContext(options);
    }
}
