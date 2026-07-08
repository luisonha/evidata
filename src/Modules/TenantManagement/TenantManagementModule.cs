using Evidata.Modules.TenantManagement.Application.Commands;
using Evidata.Modules.TenantManagement.Application.Queries;
using Evidata.Modules.TenantManagement.Domain;
using Evidata.Modules.TenantManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.TenantManagement;

public static class TenantManagementModule
{
    public static IServiceCollection AddTenantManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<EvidataDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<CreateTenantCommandHandler>();
        services.AddScoped<UpdateTenantSettingsCommandHandler>();
        services.AddScoped<ChangeTenantStatusCommandHandler>();
        services.AddScoped<GetTenantQueryHandler>();

        return services;
    }
}
