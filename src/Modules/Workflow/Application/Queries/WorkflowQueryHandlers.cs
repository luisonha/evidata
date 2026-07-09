using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Application.Queries;

public sealed record WorkflowTaskDto(
    Guid Id, Guid TenantId, string TargetModule, string TargetEntityType,
    Guid TargetEntityId, string TaskType, string Status, string Title,
    string? Description, Guid AssignedTo, DateTimeOffset? DueAt, DateTimeOffset CreatedAt)
{
    public static WorkflowTaskDto From(WorkflowTask t) => new(
        t.Id, t.TenantId, t.TargetModule, t.TargetEntityType, t.TargetEntityId,
        t.TaskType.ToString(), t.Status.ToString(), t.Title,
        t.Description, t.AssignedTo, t.DueAt, t.CreatedAt);
}

public sealed class ListWorkflowTasksQueryHandler(WorkflowDbContext db)
{
    public async Task<IReadOnlyList<WorkflowTaskDto>> HandleAsync(
        Guid tenantId, bool pendingOnly = false, CancellationToken ct = default)
    {
        var query = db.WorkflowTasks.Where(t => t.TenantId == tenantId);

        if (pendingOnly)
            query = query.Where(t =>
                t.Status == WorkflowTaskStatus.Open ||
                t.Status == WorkflowTaskStatus.InProgress);

        var items = await query
            .OrderBy(t => t.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return items.Select(WorkflowTaskDto.From).ToList();
    }
}
