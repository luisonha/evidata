using Evidata.Modules.Search.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Search.Infrastructure.Persistence.Factories;

public sealed class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql("Host=localhost;Database=evidata;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(SearchDbContextFactory).Assembly.FullName))
            .Options;

        return new SearchDbContext(options);
    }
}
