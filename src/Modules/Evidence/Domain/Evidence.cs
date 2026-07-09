using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Evidence.Domain;

/// <summary>Tipos de evidencia MVP (doc 15, sección 4).</summary>
public enum EvidenceType
{
    Policy,
    Contract,
    ConsentProof,
    LegalBasisSupport,
    SecurityMeasure,
    ReviewRecord,
    ApprovalRecord,
    GapResolution,
    SystemScreenshot,
    EmailProof,
    MeetingRecord,
    ReportArtifact,
    Other
}

/// <summary>Estados del ciclo de vida de la evidencia (doc 15, sección 5).</summary>
public enum EvidenceStatus
{
    Draft,
    Active,
    Superseded,
    Archived,
    Deleted
}

/// <summary>
/// Clasificación de sensibilidad — controla quién puede ver/descargar (doc 15, sección 6).
/// </summary>
public enum EvidenceSensitivity
{
    Public,
    Internal,
    Confidential,
    Sensitive
}

/// <summary>
/// Evidencia de cumplimiento: registro auditado que soporta decisiones regulatorias.
/// Multi-tenant, soft-delete, con clasificación de sensibilidad y relación Supersedes.
///
/// Regla: no se puede eliminar físicamente evidencia usada en versión aprobada.
/// La eliminación siempre es lógica (Status = Deleted).
/// </summary>
public class Evidence : ITenantScoped
{
    private Evidence() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public EvidenceType Type { get; private set; }
    public EvidenceStatus Status { get; private set; }
    public EvidenceSensitivity Sensitivity { get; private set; }

    /// <summary>Título descriptivo de la evidencia.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Descripción del contenido y propósito de la evidencia.</summary>
    public string? Description { get; private set; }

    /// <summary>Ruta del blob en Azure Storage (null si es evidencia sin archivo).</summary>
    public string? BlobPath { get; private set; }

    /// <summary>MIME type del archivo adjunto.</summary>
    public string? ContentType { get; private set; }

    /// <summary>Tamaño en bytes del archivo adjunto.</summary>
    public long? SizeBytes { get; private set; }

    /// <summary>Referencia a otra evidencia que este reemplaza (relación Supersedes).</summary>
    public Guid? SupersedesEvidenceId { get; private set; }

    /// <summary>Tags libres para clasificación interna del tenant (ej. "gdpr,auditoria,2024").</summary>
    public string? Tags { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Razón de la eliminación lógica (obligatoria al eliminar).</summary>
    public string? DeletionReason { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static Evidence Create(
        Guid tenantId,
        string title,
        EvidenceType type,
        EvidenceSensitivity sensitivity,
        Guid createdBy,
        string? description = null,
        string? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Evidence
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title.Trim(),
            Type = type,
            Sensitivity = sensitivity,
            Status = EvidenceStatus.Draft,
            Description = description?.Trim(),
            Tags = tags?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── State transitions ──────────────────────────────────────────────────────

    public void AttachBlob(string blobPath, string contentType, long sizeBytes, Guid updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));

        BlobPath = blobPath;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        SetUpdated(updatedBy);
    }

    public void Activate(Guid updatedBy)
    {
        if (Status != EvidenceStatus.Draft)
            throw new InvalidOperationException($"Solo se puede activar desde Draft. Estado actual: {Status}");

        Status = EvidenceStatus.Active;
        SetUpdated(updatedBy);
    }

    public void Supersede(Guid newEvidenceId, Guid updatedBy)
    {
        Status = EvidenceStatus.Superseded;
        SetUpdated(updatedBy);
    }

    public void Archive(Guid updatedBy)
    {
        Status = EvidenceStatus.Archived;
        SetUpdated(updatedBy);
    }

    /// <summary>
    /// Eliminación lógica auditada. El motivo es obligatorio.
    /// Regla: no cambia el blob en Storage — sólo marca el estado.
    /// </summary>
    public void SoftDelete(string reason, Guid deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = EvidenceStatus.Deleted;
        DeletionReason = reason.Trim();
        SetUpdated(deletedBy);
    }

    public void MarkSupersedes(Guid supersededEvidenceId, Guid updatedBy)
    {
        SupersedesEvidenceId = supersededEvidenceId;
        SetUpdated(updatedBy);
    }

    public void UpdateTags(string? tags, Guid updatedBy)
    {
        Tags = tags?.Trim();
        SetUpdated(updatedBy);
    }

    private void SetUpdated(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
