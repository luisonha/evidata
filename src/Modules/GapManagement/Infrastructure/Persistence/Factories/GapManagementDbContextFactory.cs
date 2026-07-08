using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Evidata.Modules.GapManagement.Infrastructure.Persistence.Factories;

public class GapManagementDbContextFactory : IDesignTimeDbContextFactory<GapManagementDbContext>
{
    public GapManagementDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("evidata-db")
            ?? "Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;******";

        var options = new DbContextOptionsBuilder<GapManagementDbContext>()
            .UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(GapManagementDbContextFactory).Assembly.FullName))
            .Options;

        return new GapManagementDbContext(options);
    }
}
