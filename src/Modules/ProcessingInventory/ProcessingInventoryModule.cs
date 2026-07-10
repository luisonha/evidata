using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Factories;
using Evidata.Modules.ProcessingInventory.Infrastructure.Versioning;
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

        services.AddScoped<ListProcessingActivitiesQueryHandler>();
        services.AddScoped<GetProcessingActivityQueryHandler>();
        services.AddScoped<GetProcessingActivityControlQueryHandler>();
        
        // P1-FULL-COMPOSITION: Register the control query service interface
        // The API layer will override this with the composition handler implementation
        services.AddScoped<IProcessingActivityControlQueryService>(sp =>
            sp.GetRequiredService<GetProcessingActivityControlQueryHandler>());
        
        services.AddScoped<CreateProcessingActivityCommandHandler>();
        services.AddScoped<UpdateProcessingActivityCommandHandler>();
        
        // P1-010: Register version service with IAuditService dependency
        services.AddScoped<IProcessingActivityVersionService, ProcessingActivityVersionService>();

        return services;
    }
}
