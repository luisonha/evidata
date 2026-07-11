using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Evidence.Domain;

/// <summary>
/// Review domain for evidence requirement — defines which role can validate.
/// Maps to RBAC: Legal → LegalReviewer, Security → SecurityReviewer
/// (Ref: 04-rbac-audit-evidence-gaps-contract.md, SEC-EV-001)
/// </summary>
public enum ReviewDomain
{
    Legal,
    Security
}

/// <summary>
/// A requirement for evidence linked to a ProcessingActivity node/section.
/// 
/// Encodes what type of evidence is needed (Legal review, Security review, etc.)
/// and which role can validate it, independent of file name/MIME/UI text.
/// 
/// Ref: 02-domain-implementation-contract.md, P1-005
/// </summary>
public class EvidenceRequirement : ITenantScoped
{
    private EvidenceRequirement() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    
    /// <summary>The ProcessingActivity this requirement belongs to (cross-module reference).</summary>
    public Guid ProcessingActivityId { get; private set; }
    
    /// <summary>Which domain of review — determines authorized validator role (SEC-EV-001).</summary>
    public ReviewDomain ReviewDomain { get; private set; }
    
    /// <summary>User-facing description of what evidence is required.</summary>
    public string Title { get; private set; } = default!;
    
    /// <summary>Optional extended description.</summary>
    public string? Description { get; private set; }
    
    /// <summary>True if this requirement must be fulfilled before approval/activation.</summary>
    public bool IsBlocking { get; private set; }
    
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation
    public ICollection<EvidenceValidation> Validations { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static EvidenceRequirement Create(
        Guid tenantId,
        Guid processingActivityId,
        ReviewDomain reviewDomain,
        string title,
        Guid createdBy,
        string? description = null,
        bool isBlocking = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (processingActivityId == Guid.Empty)
            throw new ArgumentException("ProcessingActivityId no puede ser Guid vacío.", nameof(processingActivityId));

        return new EvidenceRequirement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProcessingActivityId = processingActivityId,
            ReviewDomain = reviewDomain,
            Title = title.Trim(),
            Description = description?.Trim(),
            IsBlocking = isBlocking,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(string title, string? description, bool isBlocking, Guid updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        
        Title = title.Trim();
        Description = description?.Trim();
        IsBlocking = isBlocking;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
