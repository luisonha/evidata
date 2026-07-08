using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Evidence.Infrastructure.Download;

/// <summary>
/// Implementación de IEvidenceDownloadService.
/// Valida permisos básicos, genera SAS via IBlobStorageService y registra EvidenceAccessLog.
/// </summary>
public class EvidenceDownloadService : IEvidenceDownloadService
{
    private readonly EvidenceDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<EvidenceDownloadService> _logger;

    public EvidenceDownloadService(
        EvidenceDbContext db,
        IBlobStorageService blobStorage,
        ILogger<EvidenceDownloadService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<EvidenceDownloadResult> RequestDownloadAsync(
        Guid tenantId,
        Guid evidenceId,
        Guid requestedBy,
        string? reason = null,
        string? clientIp = null,
        string? userAgent = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        // Buscar la evidencia filtrando por tenant (cross-tenant safe)
        var evidence = await _db.Evidences
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Id == evidenceId)
            .FirstOrDefaultAsync(ct);

        if (evidence is null)
            throw new KeyNotFoundException(
                $"Evidencia {evidenceId} no encontrada en tenant {tenantId}.");

        if (evidence.Status == EvidenceStatus.Deleted)
            throw new InvalidOperationException(
                $"No se puede descargar evidencia eliminada (id: {evidenceId}).");

        if (string.IsNullOrWhiteSpace(evidence.BlobPath))
            throw new InvalidOperationException(
                $"La evidencia {evidenceId} no tiene archivo adjunto (BlobPath es null).");

        // Registrar log ANTES de generar SAS — garantiza auditoría incluso si SAS falla
        var accessLog = EvidenceAccessLog.Record(
            tenantId, evidenceId, requestedBy,
            evidence.Sensitivity, reason, clientIp, userAgent, correlationId);

        _db.EvidenceAccessLogs.Add(accessLog);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Descarga de evidencia {EvidenceId} solicitada por {UserId} (sensitivity={Sensitivity}) AccessLogId={LogId}",
            evidenceId, requestedBy, evidence.Sensitivity, accessLog.Id);

        // Generar SAS de lectura
        var sasResult = await _blobStorage.GenerateDownloadSasAsync(
            evidence.BlobPath,
            expiresIn: TimeSpan.FromMinutes(30),
            ct: ct);

        var fileName = ExtractFileName(evidence.BlobPath);

        return new EvidenceDownloadResult(
            sasResult.DownloadUrl,
            fileName,
            evidence.ContentType ?? "application/octet-stream",
            sasResult.ExpiresAt,
            accessLog.Id);
    }

    private static string ExtractFileName(string blobPath)
    {
        // Path format: {tenantId}/yyyy/MM/dd/{guid}_{originalName}
        var segment = blobPath.Split('/').Last();
        var underscoreIdx = segment.IndexOf('_');
        return underscoreIdx >= 0 ? segment[(underscoreIdx + 1)..] : segment;
    }
}
