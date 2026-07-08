using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Infrastructure.Evidences;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.ProcessingInventory;

public static class ProcessingInventoryModule
{
    public static IServiceCollection AddProcessingInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<ProcessingInventoryDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(ProcessingInventoryDbContextFactory).Assembly.FullName)));

        services.AddScoped<IRatEvidenceService, RatEvidenceService>();

        return services;
    }
}
