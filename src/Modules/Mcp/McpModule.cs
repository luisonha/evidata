using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Audit;
using Evidata.Modules.Mcp.Application.CitationVerification;
using Evidata.Modules.Mcp.Application.Query;
using Evidata.Modules.Mcp.Application.RatContext;
using Evidata.Modules.Mcp.Application.RiskRouting;
using Evidata.Modules.Mcp.Infrastructure.Audit;
using Evidata.Modules.Mcp.Infrastructure.CitationVerification;
using Evidata.Modules.Mcp.Infrastructure.Interactions;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Evidata.Modules.Mcp.Infrastructure.Persistence.Factories;
using Evidata.Modules.Mcp.Infrastructure.Query;
using Evidata.Modules.Mcp.Infrastructure.RatContext;
using Evidata.Modules.Mcp.Infrastructure.RiskRouting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Mcp;

public static class McpModule
{
    public static IServiceCollection AddMcpModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<McpDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(McpDbContextFactory).Assembly.FullName)));

        services.AddScoped<IMcpInteractionService, McpInteractionService>();
        services.AddSingleton<IMcpRiskRouter, McpRiskRouter>();
        services.AddScoped<IRatContextProvider, RatContextProvider>();
        services.AddScoped<IMcpAuditService, McpAuditService>();
        services.AddScoped<IMcpCitationVerifier, McpCitationVerifier>();
        services.AddScoped<IMcpQueryService, McpQueryService>();

        return services;
    }
}
