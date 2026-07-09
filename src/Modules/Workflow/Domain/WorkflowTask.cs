namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Tarea de cumplimiento asignable a un usuario sobre cualquier entidad del sistema.
/// Permite rastrear trabajo pendiente (revisiones, aprobaciones, remediaciones).
/// </summary>
public sealed class WorkflowTask
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Módulo dueño de la entidad (ej: "ProcessingInventory", "GapManagement").</summary>
    public string TargetModule { get; private set; } = default!;

    /// <summary>Tipo de entidad (ej: "ProcessingActivity", "ComplianceGap").</summary>
    public string TargetEntityType { get; private set; } = default!;

    public Guid TargetEntityId { get; private set; }

    public WorkflowTaskType TaskType { get; private set; }
    public WorkflowTaskStatus Status { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public Guid AssignedTo { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private WorkflowTask() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static WorkflowTask Create(
        Guid tenantId,
        string targetModule,
        string targetEntityType,
        Guid targetEntityId,
        WorkflowTaskType taskType,
        string title,
        Guid assignedTo,
        Guid createdBy,
        DateTimeOffset? dueAt = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEntityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new WorkflowTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TargetModule = targetModule.Trim(),
            TargetEntityType = targetEntityType.Trim(),
            TargetEntityId = targetEntityId,
            TaskType = taskType,
            Title = title.Trim(),
            Description = description?.Trim(),
            AssignedTo = assignedTo,
            CreatedBy = createdBy,
            DueAt = dueAt,
            Status = WorkflowTaskStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Transiciones FSM ──────────────────────────────────────────────────────

    /// <summary>Inicia el trabajo. Open → InProgress.</summary>
    public void Start(Guid actorId)
    {
        _ = actorId;
        if (Status != WorkflowTaskStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede iniciar una tarea en estado Open. Estado actual: {Status}.");

        Status = WorkflowTaskStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marca la tarea como completada. InProgress → Completed.</summary>
    public void Complete(Guid actorId)
    {
        _ = actorId;
        if (Status != WorkflowTaskStatus.InProgress)
            throw new InvalidOperationException(
                $"Solo se puede completar una tarea en estado InProgress. Estado actual: {Status}.");

        Status = WorkflowTaskStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Cancela la tarea. Open | InProgress → Cancelled.</summary>
    public void Cancel(Guid actorId)
    {
        _ = actorId;
        if (Status is WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled)
            throw new InvalidOperationException(
                $"No se puede cancelar una tarea en estado {Status}.");

        Status = WorkflowTaskStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marca la tarea como vencida. Open | InProgress → Overdue.</summary>
    public void MarkOverdue()
    {
        if (Status is WorkflowTaskStatus.Completed
                    or WorkflowTaskStatus.Cancelled
                    or WorkflowTaskStatus.Overdue)
            return; // idempotente

        Status = WorkflowTaskStatus.Overdue;
    }

    /// <summary>Reasigna la tarea a otro usuario. Aplica solo en Open | InProgress.</summary>
    public void Reassign(Guid newAssignee, Guid actorId)
    {
        _ = actorId;
        if (Status is WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled)
            throw new InvalidOperationException(
                $"No se puede reasignar una tarea en estado {Status}.");

        AssignedTo = newAssignee;
    }

    /// <summary>Actualiza la fecha de vencimiento. Aplica solo en Open | InProgress.</summary>
    public void UpdateDueDate(DateTimeOffset? dueAt, Guid actorId)
    {
        _ = actorId;
        if (Status is WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled)
            throw new InvalidOperationException(
                $"No se puede actualizar el vencimiento de una tarea en estado {Status}.");

        DueAt = dueAt;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Indica si la tarea está vencida respecto al tiempo actual.</summary>
    public bool IsOverdue(DateTimeOffset now) =>
        DueAt.HasValue
        && now > DueAt.Value
        && Status is WorkflowTaskStatus.Open or WorkflowTaskStatus.InProgress;
}
