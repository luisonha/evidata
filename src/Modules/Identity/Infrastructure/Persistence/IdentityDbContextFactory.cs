using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata",
            b => b.MigrationsAssembly(typeof(IdentityDbContextFactory).Assembly.FullName));
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
