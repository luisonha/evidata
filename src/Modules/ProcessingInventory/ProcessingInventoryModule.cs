using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Infrastructure.Notifications;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Factories;
using Evidata.Modules.ProcessingInventory.Infrastructure.Versioning;
using Evidata.Modules.Contracts.Notifications;
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

         // P1-017: Review event handler — called when reviews are approved
         // P1-019: Register against Contracts interface for clean DI composition
         services.AddScoped<IReviewEventHandler, ReviewEventHandler>();

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

         // P1-014-P2: Risk assessment service registration moved to Reporting module (ReportingModule.cs)
         // This consolidates cross-module service availability across all hosts (Api, fn-reporting, etc.)
         // No circular dependencies since GapManagement → ProcessingInventory already exists,
         // and Reporting (which now contains this service) doesn't exist when those older dependencies were created.
         // ProcessingActivityRiskAssessmentService is implemented in Evidata.Modules.Reporting.Infrastructure.Services.

         return services;
     }
}
