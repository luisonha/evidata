using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Query;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Mcp.Api;

[ApiController]
[Route("api/mcp")]
public sealed class McpController : ControllerBase
{
    private readonly IMcpQueryService _queryService;
    private readonly ICurrentUserContext _currentUser;

    public McpController(IMcpQueryService queryService, ICurrentUserContext currentUser)
    {
        _queryService = queryService;
        _currentUser  = currentUser;
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
    /// Lista las interacciones recientes del tenant (últimas 20).
    /// </summary>
    [HttpGet("interactions")]
    public async Task<ActionResult<IReadOnlyList<McpInteractionSummary>>> GetRecentInteractions(
        [FromServices] Evidata.Modules.Mcp.Application.Abstractions.IMcpInteractionService interactions,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var list = await interactions.GetRecentByTenantAsync(_currentUser.TenantId, limit: 20, ct);

        return Ok(list.Select(i => new McpInteractionSummary(
            i.Id, i.Question, i.RiskLevel.ToString(),
            i.RequiresHumanReview, i.UsedTenantContext, i.OccurredAt)).ToList());
    }
}

public record McpQueryApiRequest(string Question);

public record McpInteractionSummary(
    Guid Id,
    string Question,
    string RiskLevel,
    bool RequiresHumanReview,
    bool UsedTenantContext,
    DateTimeOffset OccurredAt);
