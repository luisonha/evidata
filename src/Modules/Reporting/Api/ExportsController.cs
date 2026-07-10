using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Reporting.Api;

/// <summary>
/// Export API v1 controller.
/// Endpoints: POST/GET /api/v1/processing-activities/{processingActivityId}/exports
///           GET /api/v1/exports/{exportId}
///           GET /api/v1/exports/{exportId}/download
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class ExportsController(
    IExportService exportService,
    ICurrentUserContext currentUser) : ControllerBase
{
    /// <summary>
    /// POST /api/v1/processing-activities/{processingActivityId}/exports
    /// Creates a new export request for a processing activity.
    /// Implements SEC-EXP-001: Requires valid authorization and approved activity.
    /// 
    /// Responses:
    /// - 201 Created: Export request created successfully
    /// - 400 Bad Request: Invalid ExportType
    /// - 403 Forbidden: User not authorized (SEC-EXP-001)
    /// - 422 Unprocessable Entity: Activity not in Approved/Active state (OfficialExportRequiresApproval)
    /// </summary>
    [HttpPost("processing-activities/{processingActivityId:guid}/exports")]
    [ProducesResponseType(typeof(ExportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExportErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExportErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExportErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ExportDto>> CreateExport(
        Guid processingActivityId,
        [FromBody] CreateExportRequest req,
        CancellationToken ct)
    {
        // Validate ExportType
        if (!Enum.TryParse<ExportType>(req.ExportType, ignoreCase: true, out var exportType))
            return BadRequest(new ExportErrorResponse(
                "InvalidExportType",
                $"Invalid ExportType: {req.ExportType}. Valid values: {string.Join(", ", Enum.GetNames<ExportType>())}"));

        try
        {
            // Generate correlation ID for this request
            var correlationId = Request.Headers.ContainsKey("X-Correlation-Id")
                ? Request.Headers["X-Correlation-Id"].ToString()
                : Guid.NewGuid().ToString();

            var export = await exportService.RequestExportAsync(
                tenantId: currentUser.TenantId,
                processingActivityId: processingActivityId,
                exportType: exportType,
                contentType: req.ContentType,
                requestedByUserId: currentUser.UserId,
                correlationId: correlationId,
                ct: ct);

            return CreatedAtAction(nameof(GetExport), new { exportId = export.Id }, ExportDto.From(export));
        }
        catch (UnauthorizedAccessException)
        {
            // SEC-EXP-001: User not authorized
            return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("OfficialExportRequiresApproval"))
        {
            // Business logic blocker: Activity not in Approved/Active state (SEC-EXP-001)
            return UnprocessableEntity(new ExportErrorResponse(
                "OfficialExportRequiresApproval",
                ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            // Activity not found or other operational error
            return BadRequest(new ExportErrorResponse("InvalidOperation", ex.Message));
        }
    }

    /// <summary>
    /// GET /api/v1/processing-activities/{processingActivityId}/exports
    /// Lists all exports for a processing activity.
    /// </summary>
    [HttpGet("processing-activities/{processingActivityId:guid}/exports")]
    public async Task<ActionResult<IReadOnlyList<ExportDto>>> ListActivityExports(
        Guid processingActivityId,
        CancellationToken ct)
    {
        var exports = await exportService.ListExportsByActivityAsync(processingActivityId, ct);
        return Ok(exports.Select(ExportDto.From).ToList());
    }

    /// <summary>
    /// GET /api/v1/exports/{exportId}
    /// Gets a specific export by id.
    /// </summary>
    [HttpGet("exports/{exportId:guid}")]
    public async Task<ActionResult<ExportDto>> GetExport(Guid exportId, CancellationToken ct)
    {
        var export = await exportService.GetExportAsync(exportId, ct);
        if (export is null || export.TenantId != currentUser.TenantId)
            return NotFound(new ExportErrorResponse("NotFound", "Export not found"));

        return Ok(ExportDto.From(export));
    }

    /// <summary>
    /// GET /api/v1/exports/{exportId}/download
    /// Downloads the generated export artifact.
    /// Returns 404 if export is not completed.
    /// </summary>
    [HttpGet("exports/{exportId:guid}/download")]
    public async Task<ActionResult> DownloadExport(Guid exportId, CancellationToken ct)
    {
        var downloadInfo = await exportService.GetExportDownloadAsync(
            exportId, currentUser.TenantId, currentUser.UserId, ct);

        if (downloadInfo is null)
            return NotFound(new ExportErrorResponse(
                "NotAvailable",
                "Export not found or not yet completed"));

        // TODO: Implement actual artifact retrieval from Documents module
        // This would return the actual file content with the specified content type
        return Ok(new ExportDownloadResponse(
            downloadInfo.ExportId,
            downloadInfo.ContentType,
            downloadInfo.ArtifactDocumentId));
    }
}
