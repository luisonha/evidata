using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Commands;
using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Evidence.Api;

[ApiController]
[Authorize]
[Route("api/evidence")]
[Route("api/v1/evidence")]
public class EvidenceController(
    ListEvidenceQueryHandler listHandler,
    GetEvidenceQueryHandler getHandler,
    CreateEvidenceCommandHandler createHandler,
    ValidateEvidenceCommandHandler validateHandler,
    IEvidenceDownloadService downloadService,
    ICurrentUserContext currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceDto>>> List(
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await listHandler.HandleAsync(currentUser.TenantId, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(currentUser.TenantId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> Create(
        [FromBody] CreateEvidenceRequest req, CancellationToken ct)
    {
        var cmd = new CreateEvidenceCommand(
            currentUser.TenantId, req.Title, req.Type, req.Sensitivity,
            req.Description, req.Tags, currentUser.UserId);
        var result = await createHandler.HandleAsync(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Validar o rechazar una validación de evidencia (P1-011a).
    /// Audita con AUD-EV-001 (ValidateEvidence) o AUD-EV-002 (RejectEvidence).
    /// SEC-EV-001: Solo el reviewer correcto según ReviewDomain puede validar.
    /// </summary>
    [HttpPost("{validationId:guid}/validate")]
    public async Task<ActionResult<EvidenceValidationResultDto>> ValidateEvidence(
        Guid validationId,
        [FromBody] ValidateEvidenceRequest req,
        CancellationToken ct)
    {
        var cmd = new ValidateEvidenceCommand(
            currentUser.TenantId,
            validationId,
            req.Action,
            req.Comment,
            currentUser.UserId);
        var result = await validateHandler.HandleAsync(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Descargar un archivo de evidencia (P1-015 SEC-EVDOWN-001).
    /// 
    /// Reglas:
    /// - Usuario debe tener permiso de lectura sobre la evidencia y tratamiento
    /// - Viewer NO puede descargar evidencia Sensitive/Confidential
    /// - Toda descarga debe auditarse (éxito y denegación)
    /// 
    /// Response:
    /// - 200 OK: Archivo entregado vía URL segura, AuditEvent = EvidenceDownloaded
    /// - 403 Forbidden: Usuario bloqueado (ej. Viewer + Sensitive), code = SensitiveEvidenceRestricted
    /// - 404 Not Found: Evidencia no encontrada
    /// - 422 Unprocessable Entity: Validación fallida (ej. reason faltante para Sensitive)
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(EvidenceDownloadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EvidenceDownloadDto>> Download(
        Guid id,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        try
        {
            var clientIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();

            var result = await downloadService.RequestDownloadAsync(
                currentUser.TenantId,
                id,
                currentUser.UserId,
                reason,
                clientIp,
                userAgent,
                correlationId,
                ct);

            var dto = new EvidenceDownloadDto(
                result.DownloadUrl,
                result.FileName,
                result.ContentType,
                result.ExpiresAt,
                result.AccessLogId);

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateApiError("NOT_FOUND", "error.evidence.notFound", ex.Message));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("SensitiveEvidenceRestricted"))
        {
            return Forbid();
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("Missing required reason") ||
            ex.Message.Contains("no tiene archivo"))
        {
            return UnprocessableEntity(CreateApiError(
                "VALIDATION_FAILED",
                "error.evidence.validationFailed",
                ex.Message));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("eliminada"))
        {
            return UnprocessableEntity(CreateApiError(
                "EVIDENCE_DELETED",
                "error.evidence.deleted",
                ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                CreateApiError("INTERNAL_ERROR", "error.internal", ex.Message));
        }
    }

    private ApiErrorEnvelope CreateApiError(string code, string labelKey, string message) =>
        new(new ApiErrorResponse(code, labelKey, message));
}

public record CreateEvidenceRequest(
    string Title, string Type, string Sensitivity = "Internal",
    string? Description = null, string? Tags = null);

public record ValidateEvidenceRequest(
    string Action,  // "Validate", "Reject", "MarkInsufficient"
    string? Comment = null);

/// <summary>
/// Resultado de una descarga de evidencia (para serialización HTTP).
/// </summary>
public record EvidenceDownloadDto(
    string DownloadUrl,
    string FileName,
    string ContentType,
    DateTimeOffset ExpiresAt,
    Guid AccessLogId);

/// <summary>
/// Estructura de error API estándar.
/// </summary>
public record ApiErrorEnvelope(ApiErrorResponse Error);

public record ApiErrorResponse(
    string Code,
    string LabelKey,
    string Message);
