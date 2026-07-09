using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Mcp.Infrastructure.Persistence.Factories;

public sealed class McpDbContextFactory : IDesignTimeDbContextFactory<McpDbContext>
{
    public McpDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseNpgsql("Host=localhost;Database=evidata;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(McpDbContextFactory).Assembly.FullName))
            .Options;

        return new McpDbContext(options);
    }
}
