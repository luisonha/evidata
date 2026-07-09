using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Audit;
using Evidata.Modules.Mcp.Application.CitationVerification;
using Evidata.Modules.Mcp.Application.Query;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Mcp.Api;

[ApiController]
[Route("api/mcp")]
public sealed class McpController : ControllerBase
{
    private readonly IMcpQueryService _queryService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMcpAuditService _auditService;
    private readonly IMcpInteractionService _interactions;

    public McpController(
        IMcpQueryService queryService,
        ICurrentUserContext currentUser,
        IMcpAuditService auditService,
        IMcpInteractionService interactions)
    {
        _queryService  = queryService;
        _currentUser   = currentUser;
        _auditService  = auditService;
        _interactions  = interactions;
    }

    /// <summary>
    /// Procesa una consulta de cumplimiento con contexto RAT del tenant.
    /// </summary>
    /// <response code="200">Respuesta del asistente con contexto RAT y nivel de riesgo.</response>
    /// <response code="400">Pregunta vacía o inválida.</response>
    /// <response code="401">No autenticado (headers dev ausentes en local).</response>
    [HttpPost("query")]
    public async Task<ActionResult<McpQueryResponse>> Query(
        [FromBody] McpQueryApiRequest request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("La pregunta no puede estar vacía.");

        var response = await _queryService.QueryAsync(
            new McpQueryRequest(_currentUser.TenantId, _currentUser.UserId, request.Question), ct);

        return Ok(response);
    }

    /// <summary>
    /// Historial paginado de interacciones del tenant.
    /// </summary>
    [HttpGet("interactions")]
    public async Task<ActionResult<McpInteractionHistory>> GetHistory(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? riskLevel,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated) return Unauthorized();

        var query = new McpAuditQuery(
            TenantId: _currentUser.TenantId,
            From: from,
            To: to,
            RiskLevel: Enum.TryParse<Evidata.Modules.Mcp.Domain.McpRiskLevel>(riskLevel, true, out var rl) ? rl : null,
            Status: Enum.TryParse<Evidata.Modules.Mcp.Domain.McpInteractionStatus>(status, true, out var st) ? st : null,
            Page: Math.Max(1, page),
            PageSize: Math.Clamp(pageSize, 1, 100));

        var result = await _auditService.GetInteractionHistoryAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Métricas de feedback (Helpful/NotHelpful) del tenant.
    /// </summary>
    [HttpGet("metrics/feedback")]
    public async Task<ActionResult<McpFeedbackMetrics>> GetFeedbackMetrics(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated) return Unauthorized();
        var result = await _auditService.GetFeedbackMetricsAsync(_currentUser.TenantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Resumen de revisiones HITL (open/in-progress/aprobadas/rechazadas).
    /// </summary>
    [HttpGet("metrics/hitl")]
    public async Task<ActionResult<McpHitlSummary>> GetHitlSummary(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated) return Unauthorized();
        var result = await _auditService.GetHitlSummaryAsync(_currentUser.TenantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Registra feedback (Helpful/NotHelpful) sobre una interacción.
    /// </summary>
    [HttpPost("interactions/{interactionId:guid}/feedback")]
    public async Task<IActionResult> RecordFeedback(
        Guid interactionId,
        [FromBody] McpFeedbackRequest request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated) return Unauthorized();

        await _interactions.RecordFeedbackAsync(
            interactionId, _currentUser.TenantId, _currentUser.UserId,
            request.Rating, request.Comment, ct);

        return NoContent();
    }

    /// <summary>
    /// Verifica las citaciones de una interacción contra las fuentes reales del tenant.
    /// </summary>
    [HttpPost("interactions/{interactionId:guid}/verify-citations")]
    public async Task<ActionResult<CitationVerificationReport>> VerifyCitations(
        Guid interactionId,
        [FromServices] IMcpCitationVerifier citationVerifier,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated) return Unauthorized();

        var interaction = await _interactions.GetByIdAsync(interactionId, ct);
        if (interaction is null) return NotFound();
        if (interaction.TenantId != _currentUser.TenantId) return Forbid();

        var report = await citationVerifier.VerifyAsync(
            interactionId, _currentUser.TenantId, interaction.Citations, ct);

        return Ok(report);
    }
}

public record McpQueryApiRequest(string Question);

public record McpFeedbackRequest(
    Evidata.Modules.Mcp.Domain.McpFeedbackRating Rating,
    string? Comment);
