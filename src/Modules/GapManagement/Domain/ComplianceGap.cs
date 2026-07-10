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
/// Contrato (04-rbac-audit-evidence-gaps-contract.md, sec 4):
///   Open → InCorrection → Resolved → Open (reapertura automática por reevaluación de reglas)
///   Open → AcceptedWithRisk (aceptación formal de riesgo)
///   Open → Dismissed (descarte/no aplica)
///
/// Resolved/AcceptedWithRisk → Closed (finalización formal)
///
/// Nota: Reapertura Resolved → Open es transición interna del motor de reglas sin endpoint público;
/// se audita como "AutomaticReopen" en lugar de cambio de estado manual.
/// </summary>
public enum GapStatus
{
    Open,
    InCorrection,
    Resolved,
    AcceptedWithRisk,
    Dismissed,
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

    /// <summary>
    /// Agregue una referencia a la regla que originó esta brecha (FK).
    /// Cuando una reevaluación de reglas cierra/abre gaps, se audita el cambio de regla.
    /// </summary>
    public Guid? GapRuleId { get; private set; }

    /// <summary>True si esta brecha debe bloquear la aprobación del RAT origen.</summary>
    public bool BlocksApproval => Severity == GapSeverity.Critical
        && Status == GapStatus.Open;

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
        DateTimeOffset? dueAt = null,
        Guid? gapRuleId = null)
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
            GapRuleId = gapRuleId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── FSM ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transiciona a InCorrection cuando se comienza la remediación de la brecha.
    /// Solo válido desde Open.
    /// </summary>
    public void StartCorrection(Guid modifiedBy, Guid? ownerId = null, DateTimeOffset? dueAt = null)
    {
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede iniciar corrección desde Open. Estado actual: {Status}.");

        Status = GapStatus.InCorrection;
        if (ownerId.HasValue) OwnerId = ownerId;
        if (dueAt.HasValue) DueAt = dueAt;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Resuelve la brecha (pendiente de cierre formal o reaceptación).
    /// Solo válido desde InCorrection.
    /// </summary>
    public void Resolve(Guid modifiedBy)
    {
        if (Status != GapStatus.InCorrection)
            throw new InvalidOperationException(
                $"Solo se puede resolver desde InCorrection. Estado actual: {Status}.");

        Status = GapStatus.Resolved;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Reabre automáticamente la brecha cuando la reevaluación sincrona de reglas detecta
    /// nuevamente la condición. Transición interna sin endpoint público.
    /// Solo válido desde Resolved, auditada como "AutomaticReopen".
    /// </summary>
    public void AutomaticReopen(Guid systemUserId)
    {
        if (Status != GapStatus.Resolved)
            throw new InvalidOperationException(
                $"Solo se puede reabrir automáticamente desde Resolved. Estado actual: {Status}.");

        Status = GapStatus.Open;
        Touch(systemUserId);
    }

    /// <summary>
    /// Acepta formalmente el riesgo de la brecha con justificación.
    /// Solo válido desde Open.
    /// </summary>
    public void AcceptRisk(string justification, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("La justificación de aceptación de riesgo es obligatoria.", nameof(justification));
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"No se puede aceptar riesgo desde el estado {Status}.");

        RiskAcceptanceJustification = justification.Trim();
        Status = GapStatus.AcceptedWithRisk;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Descarta o marca como no aplicable la brecha.
    /// Solo válido desde Open.
    /// </summary>
    public void Dismiss(Guid modifiedBy, string? dismissReason = null)
    {
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede descartar desde Open. Estado actual: {Status}.");

        if (!string.IsNullOrWhiteSpace(dismissReason))
            RiskAcceptanceJustification = $"Descarte: {dismissReason.Trim()}";

        Status = GapStatus.Dismissed;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Cierra definitivamente la brecha (post-resolución, post-aceptación o post-descarte).
    /// </summary>
    public void Close(Guid closedBy)
    {
        if (Status is not (GapStatus.Resolved or GapStatus.AcceptedWithRisk or GapStatus.Dismissed))
            throw new InvalidOperationException(
                $"Solo se puede cerrar desde Resolved, AcceptedWithRisk o Dismissed. Estado actual: {Status}.");

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
