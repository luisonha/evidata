using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Security.Infrastructure.Persistence;

public class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecurityDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata",
            b => b.MigrationsAssembly(typeof(SecurityDbContextFactory).Assembly.FullName));
        return new SecurityDbContext(optionsBuilder.Options);
    }
}
