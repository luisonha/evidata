using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Domain;
using Evidata.Modules.Security.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Reporting.Application.Services;

/// <summary>
/// Service for managing Export requests and operations.
/// Implements SEC-EXP-001 authorization and audit logging.
/// </summary>
public sealed class ExportService(
    IExportRepository exportRepository,
    IAuditService auditService,
    IProcessingActivityReadOnlyQueryService processingActivityQueryService,
    IResourcePermissionsQueryService resourcePermissionsService,
    ILogger<ExportService> logger) : IExportService
{
    public async Task<Export> RequestExportAsync(
        Guid tenantId,
        Guid processingActivityId,
        ExportType exportType,
        string contentType,
        Guid requestedByUserId,
        string correlationId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        // ── SEC-EXP-001: Verify user role is authorized to generate exports ────
        var permissions = await resourcePermissionsService.GetResourcePermissionsAsync(
            userId: requestedByUserId,
            tenantId: tenantId,
            resourceType: "export",
            resourceId: processingActivityId,
            ct: ct);

        // Fail-closed: Block if user lacks permission (or has blocking reasons)
        if (permissions.BlockedActions.Any(a => a.ActionCode == "GenerateOfficialExport"))
        {
            var reason = permissions.BlockedActions.First(a => a.ActionCode == "GenerateOfficialExport").ReasonCode;
            throw new UnauthorizedAccessException(
                $"User {requestedByUserId} is not authorized to generate exports. Reason: {reason}");
        }

        // ── SEC-EXP-001: Verify ProcessingActivity is in Approved state ────
        var activityStatus = await processingActivityQueryService.GetStatusAsync(
            tenantId, processingActivityId, ct);

        if (string.IsNullOrEmpty(activityStatus))
        {
            throw new InvalidOperationException(
                $"ProcessingActivity {processingActivityId} not found or not accessible in tenant {tenantId}");
        }

        // Fail-closed: Only Approved status allows export generation
        if (activityStatus != "Approved")
        {
            throw new InvalidOperationException(
                $"Cannot generate export for ProcessingActivity in state '{activityStatus}'. " +
                "Only 'Approved' state allows export generation (SEC-EXP-001).");
        }

        // Calculate next version for this activity + type combination
        var version = await exportRepository.GetNextVersionAsync(
            processingActivityId, exportType, ct);

        // Create the export aggregate
        var export = Export.Create(
            tenantId: tenantId,
            processingActivityId: processingActivityId,
            exportType: exportType,
            contentType: contentType.Trim(),
            version: version,
            requestedByUserId: requestedByUserId,
            correlationId: correlationId.Trim());

        // Persist
        await exportRepository.AddAsync(export, ct);
        await exportRepository.SaveChangesAsync(ct);

        // Log audit event: Export requested
        await auditService.LogAsync(
            tenantId: tenantId,
            userId: requestedByUserId,
            eventType: AuditEventType.GenerateOfficialExport.ToString(),
            resource: "Export",
            resourceId: export.Id,
            result: AuditEventResult.Success,
            correlationId: correlationId,
            metadata: new Dictionary<string, object?>
            {
                { "exportType", exportType.ToString() },
                { "processingActivityId", processingActivityId },
                { "version", version },
                { "contentType", contentType }
            },
            ct: ct);

        logger.LogInformation(
            "Export requested: {ExportId} for activity {ActivityId}, type {ExportType}, version {Version}, correlation {CorrelationId}",
            export.Id, processingActivityId, exportType, version, correlationId);

        return export;
    }

    public async Task StartGenerationAsync(Guid exportId, CancellationToken ct = default)
    {
        var export = await exportRepository.GetByIdAsync(exportId, ct) 
            ?? throw new InvalidOperationException($"Export {exportId} not found");

        export.Start();
        await exportRepository.UpdateAsync(export, ct);
        await exportRepository.SaveChangesAsync(ct);

        logger.LogInformation("Export generation started: {ExportId}", exportId);
    }

    public async Task CompleteAsync(Guid exportId, Guid artifactDocumentId, CancellationToken ct = default)
    {
        var export = await exportRepository.GetByIdAsync(exportId, ct) 
            ?? throw new InvalidOperationException($"Export {exportId} not found");

        export.Complete(artifactDocumentId);
        await exportRepository.UpdateAsync(export, ct);
        await exportRepository.SaveChangesAsync(ct);

        // Log audit event: Export completed
        await auditService.LogAsync(
            tenantId: export.TenantId,
            userId: export.RequestedByUserId,
            eventType: AuditEventType.GenerateOfficialExport.ToString(),
            resource: "Export",
            resourceId: export.Id,
            result: AuditEventResult.Success,
            correlationId: export.CorrelationId,
            metadata: new Dictionary<string, object?>
            {
                { "artifactDocumentId", artifactDocumentId },
                { "status", export.Status.ToString() },
                { "generatedAt", export.GeneratedAt }
            },
            ct: ct);

        logger.LogInformation(
            "Export completed: {ExportId} with artifact {ArtifactId}", 
            exportId, artifactDocumentId);
    }

    public async Task FailAsync(Guid exportId, string errorMessage, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var export = await exportRepository.GetByIdAsync(exportId, ct) 
            ?? throw new InvalidOperationException($"Export {exportId} not found");

        export.Fail(errorMessage);
        await exportRepository.UpdateAsync(export, ct);
        await exportRepository.SaveChangesAsync(ct);

        // Log audit event: Export failed
        await auditService.LogAsync(
            tenantId: export.TenantId,
            userId: export.RequestedByUserId,
            eventType: AuditEventType.GenerateOfficialExport.ToString(),
            resource: "Export",
            resourceId: export.Id,
            result: AuditEventResult.Failure,
            correlationId: export.CorrelationId,
            metadata: new Dictionary<string, object?>
            {
                { "errorMessage", errorMessage },
                { "status", export.Status.ToString() }
            },
            ct: ct);

        logger.LogWarning(
            "Export failed: {ExportId}, error: {ErrorMessage}",
            exportId, errorMessage);
    }

    public async Task AddWarningAsync(Guid exportId, string warning, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(warning);

        var export = await exportRepository.GetByIdAsync(exportId, ct) 
            ?? throw new InvalidOperationException($"Export {exportId} not found");

        export.AddWarning(warning);
        await exportRepository.UpdateAsync(export, ct);
        await exportRepository.SaveChangesAsync(ct);

        logger.LogDebug("Warning added to export {ExportId}: {Warning}", exportId, warning);
    }

    public async Task<Export?> GetExportAsync(Guid exportId, CancellationToken ct = default)
    {
        return await exportRepository.GetByIdAsync(exportId, ct);
    }

    public async Task<IReadOnlyList<Export>> ListExportsByActivityAsync(
        Guid processingActivityId, CancellationToken ct = default)
    {
        return await exportRepository.GetByProcessingActivityAsync(processingActivityId, ct);
    }

    public async Task<ExportDownload?> GetExportDownloadAsync(
        Guid exportId, Guid tenantId, Guid userId,
        CancellationToken ct = default)
    {
        var export = await exportRepository.GetByIdAsync(exportId, ct);
        if (export == null || export.TenantId != tenantId)
            return null;

        // Only return download info if export is completed
        if (export.Status != ExportStatus.Completed || export.ArtifactDocumentId == null)
            return null;

        // Log audit event: Export downloaded
        await auditService.LogAsync(
            tenantId: tenantId,
            userId: userId,
            eventType: "DownloadExport",
            resource: "Export",
            resourceId: exportId,
            result: AuditEventResult.Success,
            correlationId: export.CorrelationId,
            metadata: new Dictionary<string, object?>
            {
                { "contentType", export.ContentType },
                { "artifactDocumentId", export.ArtifactDocumentId }
            },
            ct: ct);

        return new ExportDownload(
            exportId,
            export.ContentType,
            export.ArtifactDocumentId.Value);
    }
}
