using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Evidata.Modules.GapManagement.Infrastructure.Persistence.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.GapManagement;

public static class GapManagementModule
{
    public static IServiceCollection AddGapManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<GapManagementDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(GapManagementDbContextFactory).Assembly.FullName)));

        return services;
    }
}
