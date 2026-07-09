namespace Evidata.Modules.Documents.Domain;

/// <summary>
/// Versión inmutable de un documento.
/// Cada vez que se sube una nueva versión del mismo archivo, se registra aquí.
/// El versionado tiene valor probatorio para cumplimiento Ley 21.719.
/// </summary>
public class DocumentVersion
{
    private DocumentVersion() { } // EF Core

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Número de versión (1, 2, 3...). Autoincremental por documento.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Ruta del blob de esta versión en Azure Storage.</summary>
    public string BlobPath { get; private set; } = default!;

    /// <summary>Tamaño en bytes de esta versión.</summary>
    public long SizeBytes { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public Document Document { get; private set; } = default!;

    internal static DocumentVersion Create(
        Guid documentId,
        Guid tenantId,
        string blobPath,
        long sizeBytes,
        int versionNumber,
        Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));

        return new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            TenantId = tenantId,
            BlobPath = blobPath,
            SizeBytes = sizeBytes,
            VersionNumber = versionNumber,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
