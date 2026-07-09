using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Worker.Outbox.Persistence;

public class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata",
            b => b.MigrationsAssembly(typeof(OutboxDbContextFactory).Assembly.FullName));
        return new OutboxDbContext(optionsBuilder.Options);
    }
}
