namespace Evidata.Modules.GapManagement.Domain;

/// <summary>
/// Severidad de una brecha de cumplimiento.
/// Determina urgencia, notificaciones y si bloquea aprobación del RAT.
/// </summary>
public enum GapSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Estado del ciclo de vida de una brecha.
///
/// FSM:
///   Open → Assigned → InProgress → Resolved → Closed
///   Open / Assigned / InProgress → Blocked (impedimento externo)
///   Blocked → InProgress (impedimento resuelto)
///   Open / Assigned → AcceptedRisk (riesgo aceptado formalmente)
///   Resolved / AcceptedRisk → Closed
/// </summary>
public enum GapStatus
{
    Open,
    Assigned,
    InProgress,
    Blocked,
    Resolved,
    AcceptedRisk,
    Closed
}

/// <summary>
/// Brecha de cumplimiento generada a partir de un hallazgo en cualquier módulo.
///
/// Reglas (doc 35, sec 9):
/// - Toda brecha tiene origen (módulo + entidad).
/// - Severity Critical activa el flag CriticalGapOpen en el RAT origen.
/// - Los cambios de estado se auditan (actor + timestamp).
/// - Soft delete no aplica — las brechas son registros de auditoría permanentes.
/// - La aceptación de riesgo requiere justificación explícita.
/// </summary>
public class ComplianceGap
{
    private ComplianceGap() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Módulo que originó la brecha (e.g., "ProcessingInventory", "Evidence").</summary>
    public string SourceModule { get; private set; } = default!;

    /// <summary>ID de la entidad origen (e.g., ProcessingActivity.Id).</summary>
    public Guid SourceEntityId { get; private set; }

    /// <summary>Obligación legal relacionada (opcional, referencia cross-módulo).</summary>
    public Guid? LegalObligationId { get; private set; }

    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public GapSeverity Severity { get; private set; }
    public GapStatus Status { get; private set; }

    /// <summary>Responsable asignado (null = sin asignar).</summary>
    public Guid? OwnerId { get; private set; }

    /// <summary>Fecha objetivo de cierre.</summary>
    public DateTimeOffset? DueAt { get; private set; }

    /// <summary>Justificación cuando el riesgo es aceptado formalmente.</summary>
    public string? RiskAcceptanceJustification { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? LastModifiedBy { get; private set; }
    public DateTimeOffset? LastModifiedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }

    /// <summary>True si esta brecha debe bloquear la aprobación del RAT origen.</summary>
    public bool BlocksApproval => Severity == GapSeverity.Critical
        && Status is not (GapStatus.Resolved or GapStatus.AcceptedRisk or GapStatus.Closed);

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ComplianceGap Create(
        Guid tenantId,
        string sourceModule,
        Guid sourceEntityId,
        string title,
        string description,
        GapSeverity severity,
        Guid createdBy,
        Guid? legalObligationId = null,
        DateTimeOffset? dueAt = null)
    {
        if (string.IsNullOrWhiteSpace(sourceModule))
            throw new ArgumentException("El módulo origen no puede ser vacío.", nameof(sourceModule));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título no puede ser vacío.", nameof(title));
        if (title.Length > 300)
            throw new ArgumentException("El título no puede superar 300 caracteres.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede ser vacía.", nameof(description));

        return new ComplianceGap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceModule = sourceModule.Trim(),
            SourceEntityId = sourceEntityId,
            LegalObligationId = legalObligationId,
            Title = title.Trim(),
            Description = description.Trim(),
            Severity = severity,
            Status = GapStatus.Open,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── FSM ────────────────────────────────────────────────────────────────────

    /// <summary>Asigna un responsable y transiciona a Assigned.</summary>
    public void Assign(Guid ownerId, Guid modifiedBy, DateTimeOffset? dueAt = null)
    {
        GuardNotClosed();
        if (Status is GapStatus.Resolved or GapStatus.AcceptedRisk)
            throw new InvalidOperationException(
                $"No se puede asignar una brecha en estado {Status}.");

        OwnerId = ownerId;
        if (dueAt.HasValue) DueAt = dueAt;
        Status = GapStatus.Assigned;
        Touch(modifiedBy);
    }

    /// <summary>Inicia el trabajo de remediación.</summary>
    public void StartProgress(Guid modifiedBy)
    {
        if (Status is not (GapStatus.Assigned or GapStatus.Blocked))
            throw new InvalidOperationException(
                $"Solo se puede iniciar progreso desde Assigned o Blocked. Estado actual: {Status}.");

        Status = GapStatus.InProgress;
        Touch(modifiedBy);
    }

    /// <summary>Registra un impedimento externo.</summary>
    public void Block(Guid modifiedBy)
    {
        if (Status is not (GapStatus.Open or GapStatus.Assigned or GapStatus.InProgress))
            throw new InvalidOperationException(
                $"No se puede bloquear desde el estado {Status}.");

        Status = GapStatus.Blocked;
        Touch(modifiedBy);
    }

    /// <summary>Marca la brecha como resuelta (pendiente validación).</summary>
    public void Resolve(Guid modifiedBy)
    {
        if (Status != GapStatus.InProgress)
            throw new InvalidOperationException(
                $"Solo se puede resolver desde InProgress. Estado actual: {Status}.");

        Status = GapStatus.Resolved;
        Touch(modifiedBy);
    }

    /// <summary>Acepta formalmente el riesgo con justificación.</summary>
    public void AcceptRisk(string justification, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("La justificación de aceptación de riesgo es obligatoria.", nameof(justification));
        if (Status is not (GapStatus.Open or GapStatus.Assigned or GapStatus.InProgress))
            throw new InvalidOperationException(
                $"No se puede aceptar riesgo desde el estado {Status}.");

        RiskAcceptanceJustification = justification.Trim();
        Status = GapStatus.AcceptedRisk;
        Touch(modifiedBy);
    }

    /// <summary>Cierra definitivamente la brecha (post-resolución o post-aceptación).</summary>
    public void Close(Guid closedBy)
    {
        if (Status is not (GapStatus.Resolved or GapStatus.AcceptedRisk))
            throw new InvalidOperationException(
                $"Solo se puede cerrar desde Resolved o AcceptedRisk. Estado actual: {Status}.");

        Status = GapStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        ClosedBy = closedBy;
        Touch(closedBy);
    }

    /// <summary>Actualiza campos editables (solo en estados no cerrados).</summary>
    public void Update(
        string title,
        string description,
        GapSeverity severity,
        Guid modifiedBy,
        DateTimeOffset? dueAt = null,
        Guid? legalObligationId = null)
    {
        GuardNotClosed();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título no puede ser vacío.", nameof(title));
        if (title.Length > 300)
            throw new ArgumentException("El título no puede superar 300 caracteres.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede ser vacía.", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
        Severity = severity;
        DueAt = dueAt;
        LegalObligationId = legalObligationId;
        Touch(modifiedBy);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void Touch(Guid modifiedBy)
    {
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    private void GuardNotClosed()
    {
        if (Status == GapStatus.Closed)
            throw new InvalidOperationException("Una brecha cerrada no puede modificarse.");
    }
}
