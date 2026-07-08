using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Factories;

public class ProcessingInventoryDbContextFactory : IDesignTimeDbContextFactory<ProcessingInventoryDbContext>
{
    public ProcessingInventoryDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("evidata-db")
            ?? "Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;Password=evidata_pass";

        var options = new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
            .UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(ProcessingInventoryDbContextFactory).Assembly.FullName))
            .Options;

        return new ProcessingInventoryDbContext(options);
    }
}
