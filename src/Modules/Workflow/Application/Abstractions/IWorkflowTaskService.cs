using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.Workflow.Application.Abstractions;

/// <summary>
/// Servicio de tareas de cumplimiento asignables sobre entidades de cualquier módulo.
/// </summary>
public interface IWorkflowTaskService
{
    /// <summary>Crea y asigna una nueva tarea.</summary>
    Task<WorkflowTask> CreateAsync(
        Guid tenantId,
        string targetModule,
        string targetEntityType,
        Guid targetEntityId,
        WorkflowTaskType taskType,
        string title,
        Guid assignedTo,
        Guid createdBy,
        DateTimeOffset? dueAt = null,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>Inicia el trabajo sobre la tarea.</summary>
    Task StartAsync(Guid taskId, Guid actorId, CancellationToken ct = default);

    /// <summary>Completa la tarea.</summary>
    Task CompleteAsync(Guid taskId, Guid actorId, CancellationToken ct = default);

    /// <summary>Cancela la tarea.</summary>
    Task CancelAsync(Guid taskId, Guid actorId, CancellationToken ct = default);

    /// <summary>Reasigna la tarea a otro usuario.</summary>
    Task ReassignAsync(Guid taskId, Guid newAssignee, Guid actorId, CancellationToken ct = default);

    /// <summary>Actualiza la fecha de vencimiento.</summary>
    Task UpdateDueDateAsync(Guid taskId, DateTimeOffset? dueAt, Guid actorId, CancellationToken ct = default);

    /// <summary>Marca como vencidas las tareas que superaron su DueAt.</summary>
    Task MarkOverdueTasksAsync(DateTimeOffset now, CancellationToken ct = default);

    /// <summary>Obtiene una tarea por Id.</summary>
    Task<WorkflowTask?> GetByIdAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Lista tareas abiertas asignadas a un usuario en un tenant.</summary>
    Task<IReadOnlyList<WorkflowTask>> GetOpenByAssigneeAsync(
        Guid tenantId, Guid assignedTo, CancellationToken ct = default);

    /// <summary>Lista todas las tareas de una entidad específica.</summary>
    Task<IReadOnlyList<WorkflowTask>> GetByEntityAsync(
        Guid tenantId, Guid targetEntityId, CancellationToken ct = default);
}
