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
/// Estado del ciclo de vida de una brecha (hallazgo operativo).
///
/// FSM:
///   Open → InCorrection → Resolved
///   Open → AcceptedWithRisk
///   Open → Dismissed
///   Resolved → Open (transición automática interna durante re-evaluación síncrona si regla se dispara nuevamente)
///
/// Nota: No existe endpoint público de reapertura. La reapertura es transición interna del motor de reglas.
/// </summary>
public enum GapStatus
{
    Open,
    InCorrection,
    Resolved,
    AcceptedWithRisk,
    Dismissed
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

    /// <summary>Justificación cuando el riesgo es aceptado formalmente.</summary>
    public string? RiskAcceptanceJustification { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? LastModifiedBy { get; private set; }
    public DateTimeOffset? LastModifiedAt { get; private set; }

    /// <summary>True si esta brecha debe bloquear la aprobación del RAT origen.</summary>
    public bool BlocksApproval => Severity == GapSeverity.Critical
        && Status is not (GapStatus.Resolved or GapStatus.AcceptedWithRisk or GapStatus.Dismissed);

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ComplianceGap Create(
        Guid tenantId,
        string sourceModule,
        Guid sourceEntityId,
        string title,
        string description,
        GapSeverity severity,
        Guid createdBy,
        Guid? legalObligationId = null)
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

    /// <summary>Inicia la corrección de la brecha: Open → InCorrection.</summary>
    public void StartCorrection(Guid modifiedBy)
    {
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede iniciar corrección desde Open. Estado actual: {Status}.");

        Status = GapStatus.InCorrection;
        Touch(modifiedBy);
    }

    /// <summary>Marca la brecha como resuelta después de corrección: InCorrection → Resolved.</summary>
    public void Resolve(Guid modifiedBy)
    {
        if (Status != GapStatus.InCorrection)
            throw new InvalidOperationException(
                $"Solo se puede resolver desde InCorrection. Estado actual: {Status}.");

        Status = GapStatus.Resolved;
        Touch(modifiedBy);
    }

    /// <summary>Reabre automáticamente la brecha si durante re-evaluación síncrona vuelve a dispararse: Resolved → Open.</summary>
    internal void ReopenAutomatically(Guid triggeredBySystem)
    {
        if (Status != GapStatus.Resolved)
            throw new InvalidOperationException(
                $"Solo se puede reabrir desde Resolved. Estado actual: {Status}.");

        Status = GapStatus.Open;
        // LastModifiedBy y LastModifiedAt se actualizan para auditar la reapertura automática.
        Touch(triggeredBySystem);
    }

    /// <summary>Rechaza la brecha o la desestima: Open → Dismissed.</summary>
    public void Dismiss(Guid modifiedBy)
    {
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede desestimar desde Open. Estado actual: {Status}.");

        Status = GapStatus.Dismissed;
        Touch(modifiedBy);
    }

    /// <summary>Acepta formalmente el riesgo con justificación: Open → AcceptedWithRisk.</summary>
    public void AcceptRisk(string justification, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("La justificación de aceptación de riesgo es obligatoria.", nameof(justification));
        if (Status != GapStatus.Open)
            throw new InvalidOperationException(
                $"Solo se puede aceptar riesgo desde Open. Estado actual: {Status}.");

        RiskAcceptanceJustification = justification.Trim();
        Status = GapStatus.AcceptedWithRisk;
        Touch(modifiedBy);
    }

    /// <summary>Actualiza campos editables (solo en estados no resueltos/aceptados/desestimados).</summary>
    public void Update(
        string title,
        string description,
        GapSeverity severity,
        Guid modifiedBy,
        Guid? legalObligationId = null)
    {
        if (Status is GapStatus.Resolved or GapStatus.AcceptedWithRisk or GapStatus.Dismissed)
            throw new InvalidOperationException(
                $"Una brecha en estado {Status} no puede modificarse.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título no puede ser vacío.", nameof(title));
        if (title.Length > 300)
            throw new ArgumentException("El título no puede superar 300 caracteres.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede ser vacía.", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
        Severity = severity;
        LegalObligationId = legalObligationId;
        Touch(modifiedBy);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void Touch(Guid modifiedBy)
    {
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }
}
