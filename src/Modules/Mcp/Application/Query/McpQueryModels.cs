using Evidata.Modules.Mcp.Domain;

namespace Evidata.Modules.Mcp.Application.Query;

/// <summary>
/// Petición de consulta al asistente MCP.
/// </summary>
public sealed record McpQueryRequest(
    Guid TenantId,
    Guid UserId,
    string Question);

/// <summary>
/// Respuesta del asistente MCP.
/// Incluye la respuesta textual, nivel de riesgo, flag HITL y
/// las actividades RAT que se usaron como contexto.
/// </summary>
public sealed record McpQueryResponse(
    Guid InteractionId,
    string Answer,
    McpRiskLevel RiskLevel,
    bool RequiresHumanReview,
    bool UsedTenantContext,
    IReadOnlyList<RatContextReference> RatReferences,
    string? AbstentionReason = null);

/// <summary>
/// Referencia a un RAT aprobado usado como fuente de contexto.
/// </summary>
public sealed record RatContextReference(
    Guid ActivityId,
    string Name,
    string? Department);
