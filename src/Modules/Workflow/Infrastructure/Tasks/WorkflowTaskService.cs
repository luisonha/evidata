using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Infrastructure.Tasks;

/// <summary>
/// Implementación de <see cref="IWorkflowTaskService"/> con EF Core + PostgreSQL.
/// </summary>
public sealed class WorkflowTaskService : IWorkflowTaskService
{
    private readonly WorkflowDbContext _db;

    public WorkflowTaskService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<WorkflowTask> CreateAsync(
        Guid tenantId, string targetModule, string targetEntityType, Guid targetEntityId,
        WorkflowTaskType taskType, string title, Guid assignedTo, Guid createdBy,
        DateTimeOffset? dueAt = null, string? description = null, CancellationToken ct = default)
    {
        var task = WorkflowTask.Create(tenantId, targetModule, targetEntityType, targetEntityId,
            taskType, title, assignedTo, createdBy, dueAt, description);

        _db.WorkflowTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task StartAsync(Guid taskId, Guid actorId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        task.Start(actorId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid taskId, Guid actorId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        task.Complete(actorId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid taskId, Guid actorId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        task.Cancel(actorId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReassignAsync(Guid taskId, Guid newAssignee, Guid actorId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        task.Reassign(newAssignee, actorId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateDueDateAsync(Guid taskId, DateTimeOffset? dueAt, Guid actorId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        task.UpdateDueDate(dueAt, actorId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkOverdueTasksAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var candidates = await _db.WorkflowTasks
            .Where(t => (t.Status == WorkflowTaskStatus.Open || t.Status == WorkflowTaskStatus.InProgress)
                     && t.DueAt.HasValue
                     && t.DueAt.Value < now)
            .ToListAsync(ct);

        foreach (var t in candidates)
            t.MarkOverdue();

        if (candidates.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public Task<WorkflowTask?> GetByIdAsync(Guid taskId, CancellationToken ct = default) =>
        _db.WorkflowTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);

    public async Task<IReadOnlyList<WorkflowTask>> GetOpenByAssigneeAsync(
        Guid tenantId, Guid assignedTo, CancellationToken ct = default)
    {
        return await _db.WorkflowTasks
            .Where(t => t.TenantId == tenantId
                     && t.AssignedTo == assignedTo
                     && (t.Status == WorkflowTaskStatus.Open
                      || t.Status == WorkflowTaskStatus.InProgress
                      || t.Status == WorkflowTaskStatus.Overdue))
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkflowTask>> GetByEntityAsync(
        Guid tenantId, Guid targetEntityId, CancellationToken ct = default)
    {
        return await _db.WorkflowTasks
            .Where(t => t.TenantId == tenantId && t.TargetEntityId == targetEntityId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task<WorkflowTask> GetRequiredAsync(Guid taskId, CancellationToken ct)
    {
        return await _db.WorkflowTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"WorkflowTask {taskId} no encontrada.");
    }
}
