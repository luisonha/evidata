using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Evidence.Domain;

/// <summary>
/// State machine for evidence validation cycle.
/// Ref: 04-rbac-audit-evidence-gaps-contract.md, Sección 3, P1-006
/// </summary>
public enum EvidenceValidationStatus
{
    Pending,      // Requirement created, no evidence attached yet
    Attached,     // Evidence attached, awaiting validation
    Validated,    // Evidence accepted as sufficient
    Insufficient, // Evidence attached but does not meet requirements
    Rejected      // Evidence rejected (possible due to wrong type, confidentiality, etc.)
}

/// <summary>
/// Validation record for a requirement-evidence pairing.
/// 
/// Lifecycle:
///   Pending (creation)
///   → Attached (when evidence linked)
///   → Validated | Insufficient | Rejected (by authorized reviewer per ReviewDomain)
///   
/// Must generate audit events AUD-EV-001 (validate) and AUD-EV-002 (reject).
/// Authorization must use EvidenceRequirement.ReviewDomain, NOT file name/MIME/text.
/// 
/// Ref: 02-domain-implementation-contract.md, P1-006
/// Ref: 04-rbac-audit-evidence-gaps-contract.md, Acciones críticas (AUD-EV-001, AUD-EV-002)
/// </summary>
public class EvidenceValidation : ITenantScoped
{
    private EvidenceValidation() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    
    /// <summary>The requirement this validation fulfills.</summary>
    public Guid EvidenceRequirementId { get; private set; }
    
    /// <summary>The evidence being validated (null while Pending).</summary>
    public Guid? EvidenceId { get; private set; }
    
    /// <summary>Current state in the validation cycle.</summary>
    public EvidenceValidationStatus Status { get; private set; }
    
    /// <summary>
    /// User feedback on validation (e.g., "Policy document lacks retention clause"
    /// or "Security measures correctly documented").
    /// </summary>
    public string? ValidationComment { get; private set; }
    
    /// <summary>User who last changed the validation status (validator/reviewer).</summary>
    public Guid? ValidatedBy { get; private set; }
    
    /// <summary>Timestamp of the last state transition (especially Validated/Rejected).</summary>
    public DateTimeOffset? ValidatedAt { get; private set; }
    
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation
    public EvidenceRequirement EvidenceRequirement { get; private set; } = default!;
    public Evidence? Evidence { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static EvidenceValidation Create(
        Guid tenantId,
        Guid evidenceRequirementId,
        Guid createdBy)
    {
        if (evidenceRequirementId == Guid.Empty)
            throw new ArgumentException("EvidenceRequirementId no puede ser Guid vacío.", nameof(evidenceRequirementId));

        return new EvidenceValidation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EvidenceRequirementId = evidenceRequirementId,
            EvidenceId = null,
            Status = EvidenceValidationStatus.Pending,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── State transitions ──────────────────────────────────────────────────────

    /// <summary>
    /// Attach evidence to this validation (Pending → Attached).
    /// Rule: Cannot attach twice; detachment is not supported in MVP.
    /// </summary>
    public void AttachEvidence(Guid evidenceId, Guid updatedBy)
    {
        if (evidenceId == Guid.Empty)
            throw new ArgumentException("EvidenceId no puede ser Guid vacío.", nameof(evidenceId));

        if (Status != EvidenceValidationStatus.Pending)
            throw new InvalidOperationException(
                $"Solo se puede adjuntar evidencia desde estado Pending. Estado actual: {Status}");

        if (EvidenceId.HasValue)
            throw new InvalidOperationException("Esta validación ya tiene evidencia adjunta.");

        EvidenceId = evidenceId;
        Status = EvidenceValidationStatus.Attached;
        SetUpdated(updatedBy);
    }

    /// <summary>
    /// Validate the attached evidence as sufficient (Attached → Validated).
    /// Rule: Requires evidence already attached.
    /// </summary>
    public void Validate(string? comment, Guid validatedBy)
    {
        if (Status != EvidenceValidationStatus.Attached)
            throw new InvalidOperationException(
                $"Solo se puede validar desde estado Attached. Estado actual: {Status}");

        if (!EvidenceId.HasValue)
            throw new InvalidOperationException("No hay evidencia adjunta para validar.");

        Status = EvidenceValidationStatus.Validated;
        ValidationComment = comment?.Trim();
        ValidatedBy = validatedBy;
        ValidatedAt = DateTimeOffset.UtcNow;
        SetUpdated(validatedBy);
    }

    /// <summary>
    /// Mark evidence as insufficient (Attached → Insufficient).
    /// Rule: Requires evidence already attached.
    /// </summary>
    public void MarkInsufficient(string? comment, Guid reviewedBy)
    {
        if (Status != EvidenceValidationStatus.Attached)
            throw new InvalidOperationException(
                $"Solo se puede marcar insuficiente desde estado Attached. Estado actual: {Status}");

        if (!EvidenceId.HasValue)
            throw new InvalidOperationException("No hay evidencia adjunta para rechazar.");

        Status = EvidenceValidationStatus.Insufficient;
        ValidationComment = comment?.Trim();
        ValidatedBy = reviewedBy;
        ValidatedAt = DateTimeOffset.UtcNow;
        SetUpdated(reviewedBy);
    }

    /// <summary>
    /// Reject the validation (Attached → Rejected).
    /// Rule: Requires evidence already attached.
    /// </summary>
    public void Reject(string? comment, Guid reviewedBy)
    {
        if (Status != EvidenceValidationStatus.Attached)
            throw new InvalidOperationException(
                $"Solo se puede rechazar desde estado Attached. Estado actual: {Status}");

        if (!EvidenceId.HasValue)
            throw new InvalidOperationException("No hay evidencia adjunta para rechazar.");

        Status = EvidenceValidationStatus.Rejected;
        ValidationComment = comment?.Trim();
        ValidatedBy = reviewedBy;
        ValidatedAt = DateTimeOffset.UtcNow;
        SetUpdated(reviewedBy);
    }

    private void SetUpdated(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
