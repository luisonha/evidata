using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Documents.Infrastructure.Persistence;

/// <summary>
/// Factory para EF Core design-time (migraciones).
/// Usa la connection string de development (Aspire/Docker PostgreSQL).
/// </summary>
public class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata_dev",
                b => b.MigrationsAssembly(typeof(DocumentDbContextFactory).Assembly.FullName))
            .Options;

        return new DocumentDbContext(options);
    }
}
