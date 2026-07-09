namespace Evidata.Modules.Evidence.Domain;

/// <summary>Estado del trabajo de empaquetado.</summary>
public enum EvidencePackStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Trabajo asíncrono para generar un paquete ZIP de múltiples evidencias.
///
/// Flujo (doc 15, sec 10):
/// 1. API crea EvidencePackJob en estado Pending y encola un mensaje.
/// 2. Azure Function procesa el job: descarga blobs, empaqueta en ZIP, sube al storage.
/// 3. Job pasa a Completed con BlobPath del ZIP resultante.
/// 4. Cliente descarga el ZIP via SAS generado post-completion.
///
/// Reglas:
/// - Máximo 50 evidencias por pack (protección contra packs gigantes).
/// - Solo evidencias Active del mismo tenant.
/// - El ZIP vive 7 días (TTL, limpiado por Maintenance Function).
/// </summary>
public class EvidencePackJob
{
    public const int MaxEvidencesPerPack = 50;
    public const int ZipTtlDays = 7;

    private readonly List<Guid> _evidenceIds = [];

    private EvidencePackJob() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }

    public EvidencePackStatus Status { get; private set; }

    /// <summary>IDs de evidencias a empaquetar (snapshot al momento de creación).</summary>
    public IReadOnlyList<Guid> EvidenceIds => _evidenceIds.AsReadOnly();

    /// <summary>Ruta del ZIP en blob storage (disponible cuando Status == Completed).</summary>
    public string? ResultBlobPath { get; private set; }

    /// <summary>Mensaje de error (disponible cuando Status == Failed).</summary>
    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Expira 7 días después de completarse.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static EvidencePackJob Create(
        Guid tenantId,
        Guid requestedBy,
        IEnumerable<Guid> evidenceIds)
    {
        var ids = evidenceIds.Distinct().ToList();

        if (ids.Count == 0)
            throw new ArgumentException("El pack debe contener al menos una evidencia.", nameof(evidenceIds));

        if (ids.Count > MaxEvidencesPerPack)
            throw new ArgumentException(
                $"El pack no puede contener más de {MaxEvidencesPerPack} evidencias (solicitadas: {ids.Count}).",
                nameof(evidenceIds));

        var job = new EvidencePackJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestedBy = requestedBy,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = EvidencePackStatus.Pending
        };
        job._evidenceIds.AddRange(ids);
        return job;
    }

    // ── FSM transitions ───────────────────────────────────────────────────────

    public void MarkProcessing()
    {
        if (Status != EvidencePackStatus.Pending)
            throw new InvalidOperationException($"No se puede iniciar un job en estado {Status}.");
        Status = EvidencePackStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string resultBlobPath)
    {
        if (Status != EvidencePackStatus.Processing)
            throw new InvalidOperationException($"No se puede completar un job en estado {Status}.");
        if (string.IsNullOrWhiteSpace(resultBlobPath))
            throw new ArgumentException("ResultBlobPath no puede ser vacío.", nameof(resultBlobPath));

        Status = EvidencePackStatus.Completed;
        ResultBlobPath = resultBlobPath;
        CompletedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(ZipTtlDays);
    }

    public void MarkFailed(string errorMessage)
    {
        if (Status == EvidencePackStatus.Completed)
            throw new InvalidOperationException("No se puede fallar un job ya completado.");

        Status = EvidencePackStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
