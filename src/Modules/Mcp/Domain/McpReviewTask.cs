namespace Evidata.Modules.Mcp.Domain;

/// <summary>
/// Tarea de revisión humana (HITL) generada cuando una <see cref="McpInteraction"/>
/// tiene <c>RequiresHumanReview = true</c>.
/// FSM: Open → InProgress → Approved | Rejected.
/// </summary>
public sealed class McpReviewTask
{
    public Guid Id { get; private set; }
    public Guid InteractionId { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Revisor asignado para la tarea (null hasta que se asigne).</summary>
    public Guid? AssignedTo { get; private set; }

    public McpReviewTaskStatus Status { get; private set; }

    /// <summary>Decisión del revisor (Approved/Rejected) con justificación.</summary>
    public string? ReviewNotes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private McpReviewTask() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static McpReviewTask Create(Guid interactionId, Guid tenantId)
    {
        return new McpReviewTask
        {
            Id = Guid.NewGuid(),
            InteractionId = interactionId,
            TenantId = tenantId,
            Status = McpReviewTaskStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Transiciones FSM ──────────────────────────────────────────────────────

    /// <summary>Asigna revisor e inicia la tarea. Open → InProgress.</summary>
    public void Start(Guid reviewerId)
    {
        if (Status != McpReviewTaskStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede iniciar una tarea en estado Open. Estado actual: {Status}.");

        AssignedTo = reviewerId;
        Status = McpReviewTaskStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El revisor aprueba la respuesta del asistente. InProgress → Approved.</summary>
    public void Approve(Guid reviewerId, string? notes = null)
    {
        EnsureInProgress(reviewerId);
        ReviewNotes = notes?.Trim();
        Status = McpReviewTaskStatus.Approved;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El revisor rechaza la respuesta. InProgress → Rejected.</summary>
    public void Reject(Guid reviewerId, string notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);
        EnsureInProgress(reviewerId);
        ReviewNotes = notes.Trim();
        Status = McpReviewTaskStatus.Rejected;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureInProgress(Guid reviewerId)
    {
        if (Status != McpReviewTaskStatus.InProgress)
            throw new InvalidOperationException(
                $"La acción requiere estado InProgress. Estado actual: {Status}.");
        if (AssignedTo.HasValue && AssignedTo != reviewerId)
            throw new InvalidOperationException(
                "Solo el revisor asignado puede completar la tarea.");
    }
}
