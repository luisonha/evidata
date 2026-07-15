using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Application.Queries;
using Evidata.Modules.Reporting.Application.Services;
using Evidata.Modules.Reporting.Infrastructure.Adapters;
using Evidata.Modules.Reporting.Infrastructure.Jobs;
using Evidata.Modules.Reporting.Infrastructure.Persistence;
using Evidata.Modules.Reporting.Infrastructure.Persistence.Factories;
using Evidata.Modules.Reporting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(ReportingDbContextFactory).Assembly.FullName)));

        services.AddScoped<IReportJobService, ReportJobService>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<GetExportOptionsQueryHandler>();
        services.AddScoped<IExportOptionsQueryService>(sp => sp.GetRequiredService<GetExportOptionsQueryHandler>());

        // SEC-EXP-001: ProcessingActivity adapter for validating approved state before export generation.
        // Centralized here to eliminate duplication across hosts (Api, fn-reporting, etc.)
        services.AddScoped<GetProcessingActivityQueryHandler>();
        services.AddScoped<IProcessingActivityReadOnlyQueryService>(sp =>
            new ProcessingActivityReadOnlyQueryAdapter(sp.GetRequiredService<GetProcessingActivityQueryHandler>()));

        // P1-014-P2: Register risk assessment service for export warning auto-detection
        // This service aggregates cross-module risk data from GapManagement, Evidence, and Workflow.
        // Consolidated in Reporting module (same pattern as ProcessingActivityReadOnlyQueryAdapter above)
        // to eliminate duplication across hosts (Api, fn-reporting, etc.) and avoid manual registration.
        services.AddScoped<ProcessingActivityRiskAssessmentService>();
        services.AddScoped<IProcessingActivityRiskAssessmentService>(sp =>
            sp.GetRequiredService<ProcessingActivityRiskAssessmentService>());

        return services;
    }
}
