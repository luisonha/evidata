using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;

public class LegalKnowledgeDbContextFactory : IDesignTimeDbContextFactory<LegalKnowledgeDbContext>
{
    public LegalKnowledgeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LegalKnowledgeDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata_dev",
                b => b.MigrationsAssembly(typeof(LegalKnowledgeDbContextFactory).Assembly.FullName))
            .Options;

        return new LegalKnowledgeDbContext(options);
    }
}
