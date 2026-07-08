using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Documents.Domain;

/// <summary>
/// Estado del ciclo de vida de un documento.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Subida solicitada, esperando que el cliente complete el upload a Blob Storage.</summary>
    PendingUpload,
    /// <summary>Blob confirmado, pendiente procesamiento por Azure Function.</summary>
    Uploaded,
    /// <summary>En proceso de extracción de texto e indexación.</summary>
    Processing,
    /// <summary>Procesamiento completado correctamente.</summary>
    Processed,
    /// <summary>Procesamiento falló (ver DocumentProcessingJob para detalle).</summary>
    Failed
}

/// <summary>
/// Entidad central del módulo Documents.
/// Representa un documento legal/evidencia con sus metadatos.
/// Implementa ITenantScoped para aislamiento multi-tenant via global EF filter.
/// Implementa soft delete (DeletedAt).
/// </summary>
public class Document : ITenantScoped
{
    private Document() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Nombre original del archivo subido por el usuario.</summary>
    public string OriginalFileName { get; private set; } = default!;

    /// <summary>MIME type del archivo (application/pdf, image/png, etc.).</summary>
    public string ContentType { get; private set; } = default!;

    /// <summary>Tamaño en bytes. Null hasta que el upload se confirma.</summary>
    public long? SizeBytes { get; private set; }

    /// <summary>
    /// Ruta del blob en Azure Storage ({tenantId}/yyyy/MM/dd/{guid}_{file}).
    /// Null hasta que GenerateUploadSas es llamado.
    /// </summary>
    public string? BlobPath { get; private set; }

    /// <summary>Estado actual del documento.</summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>Descripción opcional asignada por el usuario.</summary>
    public string? Description { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Null = activo. Populated = soft deleted.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();
    private readonly List<DocumentVersion> _versions = new();

    // ── Factory ────────────────────────────────────────────────────────────────

    public static Document Create(
        Guid tenantId,
        string originalFileName,
        string contentType,
        Guid createdBy,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return new Document
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            Status = DocumentStatus.PendingUpload,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── State transitions ──────────────────────────────────────────────────────

    public void SetBlobPath(string blobPath, long sizeBytes, Guid updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        BlobPath = blobPath;
        SizeBytes = sizeBytes;
        Status = DocumentStatus.Uploaded;
        SetUpdated(updatedBy);
    }

    public void MarkProcessing(Guid updatedBy)
    {
        Status = DocumentStatus.Processing;
        SetUpdated(updatedBy);
    }

    public void MarkProcessed(Guid updatedBy)
    {
        Status = DocumentStatus.Processed;
        SetUpdated(updatedBy);
    }

    public void MarkFailed(Guid updatedBy)
    {
        Status = DocumentStatus.Failed;
        SetUpdated(updatedBy);
    }

    public void SoftDelete(Guid deletedBy)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        SetUpdated(deletedBy);
    }

    public DocumentVersion AddVersion(string blobPath, long sizeBytes, int versionNumber, Guid createdBy)
    {
        var version = DocumentVersion.Create(Id, TenantId, blobPath, sizeBytes, versionNumber, createdBy);
        _versions.Add(version);
        SetUpdated(createdBy);
        return version;
    }

    private void SetUpdated(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
