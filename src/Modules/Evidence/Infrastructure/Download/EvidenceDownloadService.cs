using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Evidata.Modules.Security.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Evidence.Infrastructure.Download;

/// <summary>
/// Implementación de IEvidenceDownloadService.
/// Valida permisos ANTES de generar SAS, registra auditoría de éxito/denegación.
///
/// P1-015 SEC-EVDOWN-001: 
/// - PRIMERO valida autorización (IsBlocked_DownloadSensitiveEvidence + reason para Sensitive)
/// - Falla con 403/422 ANTES de generar SAS
/// - Registra AuditEvent.EvidenceDownloaded (éxito) o EvidenceAccessDenied (denegación)
/// </summary>
public class EvidenceDownloadService : IEvidenceDownloadService
{
    private readonly EvidenceDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly IResourcePermissionsQueryService _permissionsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<EvidenceDownloadService> _logger;

    public EvidenceDownloadService(
        EvidenceDbContext db,
        IBlobStorageService blobStorage,
        IResourcePermissionsQueryService permissionsService,
        IAuditService auditService,
        ILogger<EvidenceDownloadService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _permissionsService = permissionsService;
        _auditService = auditService;
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

        // ═══════════════════════════════════════════════════════════════════════════════
        // GAP 1 + 3: VALIDAR AUTORIZACIÓN ANTES DE GENERAR SAS (fail-closed)
        // ═══════════════════════════════════════════════════════════════════════════════

        // GAP 3: Consultar ResourcePermissionsQueryService para detectar Viewer
        var context = new ResourceContextData(
            CustomFlags: new Dictionary<string, object>
            {
                { "evidenceSensitivity", evidence.Sensitivity.ToString() }
            });

        var permissions = await _permissionsService.GetResourcePermissionsAsync(
            requestedBy, tenantId, "evidence", evidenceId, context, ct);

        var isDownloadBlocked = permissions.BlockedActions
            .Any(ba => ba.ActionCode == "DownloadEvidence");

        if (isDownloadBlocked)
        {
            // Usuario es Viewer intenta descargar evidencia sensible → 403 SensitiveEvidenceRestricted
            _logger.LogWarning(
                "Acceso denegado: Viewer intenta descargar evidencia sensible {EvidenceId} (TenantId={TenantId})",
                evidenceId, tenantId);

            // Registrar denegación (antes de lanzar excepción)
            var denyMetadata = new Dictionary<string, object?>
            {
                { "evidenceId", evidenceId },
                { "sensitivity", evidence.Sensitivity.ToString() },
                { "reason", "Viewer cannot download sensitive evidence" }
            };

            await _auditService.LogAsync(
                tenantId,
                requestedBy,
                "EvidenceAccessDenied",
                "Evidence",
                evidenceId,
                AuditEventResult.Blocked,
                correlationId,
                denyMetadata,
                ipAddress: clientIp,
                severity: AuditSeverity.Critical,
                ct: ct);

            throw new InvalidOperationException(
                "SensitiveEvidenceRestricted: Viewer cannot download sensitive evidence.");
        }

        // GAP 4: Validar que reason sea obligatorio para Sensitive ANTES de crear el log
        if (evidence.Sensitivity == EvidenceSensitivity.Sensitive && string.IsNullOrWhiteSpace(reason))
        {
            _logger.LogWarning(
                "Validación fallida: reason obligatorio para evidencia Sensitive {EvidenceId}",
                evidenceId);

            // Registrar denegación
            var denyMetadata = new Dictionary<string, object?>
            {
                { "evidenceId", evidenceId },
                { "sensitivity", evidence.Sensitivity.ToString() },
                { "reason", "Missing required reason for Sensitive evidence" }
            };

            await _auditService.LogAsync(
                tenantId,
                requestedBy,
                "EvidenceAccessDenied",
                "Evidence",
                evidenceId,
                AuditEventResult.Blocked,
                correlationId,
                denyMetadata,
                ipAddress: clientIp,
                severity: AuditSeverity.Critical,
                ct: ct);

            throw new InvalidOperationException(
                "Missing required reason for Sensitive evidence download.");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // GAP 2: REGISTRAR LOG DE ACCESO + AUDITORÍA DE ÉXITO (fail-safe)
        // ═══════════════════════════════════════════════════════════════════════════════

        // Registrar log ANTES de generar SAS — garantiza auditoría incluso si SAS falla
        var accessLog = EvidenceAccessLog.Record(
            tenantId, evidenceId, requestedBy,
            evidence.Sensitivity, reason, clientIp, userAgent, correlationId);

        _db.EvidenceAccessLogs.Add(accessLog);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Descarga de evidencia {EvidenceId} solicitada por {UserId} (sensitivity={Sensitivity}) AccessLogId={LogId}",
            evidenceId, requestedBy, evidence.Sensitivity, accessLog.Id);

        try
        {
            // Generar SAS de lectura
            var sasResult = await _blobStorage.GenerateDownloadSasAsync(
                evidence.BlobPath,
                expiresIn: TimeSpan.FromMinutes(30),
                ct: ct);

            var fileName = ExtractFileName(evidence.BlobPath);

            // Registrar auditoría de éxito
            var successMetadata = new Dictionary<string, object?>
            {
                { "evidenceId", evidenceId },
                { "sensitivity", evidence.Sensitivity.ToString() },
                { "accessLogId", accessLog.Id },
                { "sasExpiresAt", sasResult.ExpiresAt.ToString("O") },
                { "clientIp", clientIp },
                { "userAgent", userAgent?.Length > 100 ? userAgent[..100] : userAgent }
            };

            await _auditService.LogAsync(
                tenantId,
                requestedBy,
                "EvidenceDownloaded",
                "Evidence",
                evidenceId,
                AuditEventResult.Success,
                correlationId,
                successMetadata,
                ipAddress: clientIp,
                severity: AuditSeverity.Info,
                ct: ct);

            return new EvidenceDownloadResult(
                sasResult.DownloadUrl,
                fileName,
                evidence.ContentType ?? "application/octet-stream",
                sasResult.ExpiresAt,
                accessLog.Id);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("SensitiveEvidenceRestricted")))
        {
            // Log error en auditoría (no es denegación de autorización, es error técnico)
            _logger.LogError(ex, "Error generando SAS para evidencia {EvidenceId}", evidenceId);

            var errorMetadata = new Dictionary<string, object?>
            {
                { "evidenceId", evidenceId },
                { "error", ex.Message },
                { "accessLogId", accessLog.Id }
            };

            await _auditService.LogAsync(
                tenantId,
                requestedBy,
                "EvidenceDownloaded",
                "Evidence",
                evidenceId,
                AuditEventResult.Failure,
                correlationId,
                errorMetadata,
                ipAddress: clientIp,
                severity: AuditSeverity.Critical,
                ct: ct);

            throw;
        }
    }

    private static string ExtractFileName(string blobPath)
    {
        // Path format: {tenantId}/yyyy/MM/dd/{guid}_{originalName}
        var segment = blobPath.Split('/').Last();
        var underscoreIdx = segment.IndexOf('_');
        return underscoreIdx >= 0 ? segment[(underscoreIdx + 1)..] : segment;
    }
}
