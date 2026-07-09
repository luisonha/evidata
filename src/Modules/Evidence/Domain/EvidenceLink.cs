namespace Evidata.Modules.Evidence.Domain;

/// <summary>
/// Tipo de entidad a la que se vincula la evidencia.
/// Permite asociar evidencias a obligaciones legales, documentos, controles, etc.
/// </summary>
public enum LinkedEntityType
{
    LegalObligation,
    Document,
    SecurityControl,
    DataSubjectRequest,
    AuditFinding,
    ProcessingActivity,
    Other
}

/// <summary>
/// Vínculo entre una evidencia y otra entidad del sistema (cross-módulo).
///
/// Reglas (doc 15, sec 9):
/// - Una evidencia puede tener múltiples links a distintas entidades.
/// - El vínculo es soft-deleteable (deletedAt).
/// - No hay FK real cross-módulo: solo se almacena el ID + tipo.
/// - Append-only en producción; la eliminación lógica preserva auditoría.
/// </summary>
public class EvidenceLink
{
    private EvidenceLink() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid EvidenceId { get; private set; }

    /// <summary>Tipo de la entidad vinculada.</summary>
    public LinkedEntityType LinkedEntityType { get; private set; }

    /// <summary>ID de la entidad vinculada (sin FK real — cross-módulo).</summary>
    public Guid LinkedEntityId { get; private set; }

    /// <summary>Nota opcional sobre el propósito del vínculo.</summary>
    public string? Note { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Soft delete — null = activo.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    // Navigation
    public Evidence Evidence { get; private set; } = default!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static EvidenceLink Create(
        Guid tenantId,
        Guid evidenceId,
        LinkedEntityType linkedEntityType,
        Guid linkedEntityId,
        Guid createdBy,
        string? note = null)
    {
        if (linkedEntityId == Guid.Empty)
            throw new ArgumentException("LinkedEntityId no puede ser Guid vacío.", nameof(linkedEntityId));

        return new EvidenceLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EvidenceId = evidenceId,
            LinkedEntityType = linkedEntityType,
            LinkedEntityId = linkedEntityId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            Note = note?.Trim()
        };
    }

    // ── Soft delete ────────────────────────────────────────────────────────────

    public void Delete(Guid deletedBy)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException($"EvidenceLink {Id} ya está eliminado.");

        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    public bool IsActive => DeletedAt is null;
}
