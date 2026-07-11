using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Commands;
using Evidata.Modules.Workflow.Application.Queries;
using Evidata.Modules.Workflow.Infrastructure;
using Evidata.Modules.Workflow.Infrastructure.Notifications;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Infrastructure.Persistence.Factories;
using Evidata.Modules.Workflow.Infrastructure.Policy;
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

        // Notifications service — emits events to Outbox
        services.AddScoped<IReviewNotificationService, OutboxReviewNotificationService>();
        
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReviewRequirementPolicyService, ReviewRequirementPolicyService>();
        services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();
        services.AddScoped<SetReviewRequirementCommandHandler>();
        services.AddScoped<ListWorkflowTasksQueryHandler>();
        services.AddScoped<GetReviewSummaryQueryHandler>();
        services.AddScoped<IReviewSummaryQueryService, ReviewSummaryQueryService>();

        return services;
    }
}
