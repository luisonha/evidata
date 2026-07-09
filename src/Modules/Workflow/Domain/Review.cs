namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Representa una revisión humana formal sobre una entidad de cualquier módulo.
/// La entidad es inmutable en sus datos históricos — una vez Approved o ChangesRequested
/// no puede revertirse; se crea una nueva revisión si se necesita otro ciclo.
/// </summary>
public sealed class Review
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Módulo dueño de la entidad revisada (ej: "ProcessingInventory").</summary>
    public string TargetModule { get; private set; } = default!;

    /// <summary>Tipo de entidad revisada (ej: "ProcessingActivity").</summary>
    public string TargetEntityType { get; private set; } = default!;

    /// <summary>Id de la entidad revisada.</summary>
    public Guid TargetEntityId { get; private set; }

    public ReviewStatus Status { get; private set; }

    public Guid RequestedBy { get; private set; }
    public Guid? ReviewerId { get; private set; }
    public string? Comments { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Review() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Review Create(
        Guid tenantId,
        string targetModule,
        string targetEntityType,
        Guid targetEntityId,
        Guid requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEntityType);

        return new Review
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TargetModule = targetModule.Trim(),
            TargetEntityType = targetEntityType.Trim(),
            TargetEntityId = targetEntityId,
            Status = ReviewStatus.Open,
            RequestedBy = requestedBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Transiciones FSM ──────────────────────────────────────────────────────

    /// <summary>Asigna un revisor e inicia el proceso. Open → InProgress.</summary>
    public void Start(Guid reviewerId)
    {
        if (Status != ReviewStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede iniciar una revisión en estado Open. Estado actual: {Status}.");

        ReviewerId = reviewerId;
        Status = ReviewStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El revisor aprueba. InProgress → Approved.</summary>
    public void Approve(Guid reviewerId, string? comments = null)
    {
        EnsureInProgress();
        EnsureSameReviewer(reviewerId);

        Comments = comments?.Trim();
        Status = ReviewStatus.Approved;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El revisor solicita cambios. InProgress → ChangesRequested.</summary>
    public void RequestChanges(Guid reviewerId, string comments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comments);
        EnsureInProgress();
        EnsureSameReviewer(reviewerId);

        Comments = comments.Trim();
        Status = ReviewStatus.ChangesRequested;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Cancela la revisión. Open | InProgress → Cancelled.</summary>
    public void Cancel(Guid cancelledBy)
    {
        if (Status is ReviewStatus.Approved or ReviewStatus.ChangesRequested or ReviewStatus.Cancelled)
            throw new InvalidOperationException(
                $"No se puede cancelar una revisión en estado {Status}.");

        _ = cancelledBy; // auditado externamente
        Status = ReviewStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureInProgress()
    {
        if (Status != ReviewStatus.InProgress)
            throw new InvalidOperationException(
                $"La acción requiere estado InProgress. Estado actual: {Status}.");
    }

    private void EnsureSameReviewer(Guid reviewerId)
    {
        if (ReviewerId.HasValue && ReviewerId != reviewerId)
            throw new InvalidOperationException(
                "Solo el revisor asignado puede completar la revisión.");
    }
}
