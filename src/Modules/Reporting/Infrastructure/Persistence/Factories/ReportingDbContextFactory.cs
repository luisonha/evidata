using Evidata.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Reporting.Infrastructure.Persistence.Factories;

public sealed class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseNpgsql("Host=localhost;Database=evidata;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(ReportingDbContextFactory).Assembly.FullName))
            .Options;

        return new ReportingDbContext(options);
    }
}
