using Evidata.Modules.Search.Application.Abstractions;
using Evidata.Modules.Search.Infrastructure.Persistence;
using Evidata.Modules.Search.Infrastructure.Persistence.Factories;
using Evidata.Modules.Search.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Search;

public static class SearchModule
{
    public static IServiceCollection AddSearchModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<SearchDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(SearchDbContextFactory).Assembly.FullName)));

        services.AddScoped<ISearchQueryLogService, SearchQueryLogService>();

        return services;
    }
}
