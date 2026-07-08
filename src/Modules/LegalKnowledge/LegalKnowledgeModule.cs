using Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.LegalKnowledge;

public static class LegalKnowledgeModule
{
    public static IServiceCollection AddLegalKnowledge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<LegalKnowledgeDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(LegalKnowledgeDbContextFactory).Assembly.FullName)));

        return services;
    }
}
