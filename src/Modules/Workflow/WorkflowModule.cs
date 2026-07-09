using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Queries;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Infrastructure.Persistence.Factories;
using Evidata.Modules.Workflow.Infrastructure.Reviews;
using Evidata.Modules.Workflow.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Workflow;

public static class WorkflowModule
{
    public static IServiceCollection AddWorkflowModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(WorkflowDbContextFactory).Assembly.FullName)));

        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();
        services.AddScoped<ListWorkflowTasksQueryHandler>();

        return services;
    }
}
