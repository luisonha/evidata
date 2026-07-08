using Evidata.Modules.Mcp.Domain;

namespace Evidata.Modules.Mcp.Application.Audit;

/// <summary>Filtros para consulta de historial MCP.</summary>
public sealed record McpAuditQuery(
    Guid TenantId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    McpRiskLevel? RiskLevel = null,
    McpInteractionStatus? Status = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>Entrada de historial de una interacción MCP.</summary>
public sealed record McpInteractionEntry(
    Guid InteractionId,
    Guid UserId,
    string Question,
    McpRiskLevel RiskLevel,
    McpInteractionStatus Status,
    bool UsedTenantContext,
    bool RequiresHumanReview,
    int CitationCount,
    DateTimeOffset OccurredAt);

/// <summary>Resultado paginado del historial de interacciones.</summary>
public sealed record McpInteractionHistory(
    IReadOnlyList<McpInteractionEntry> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>Métricas de feedback por tenant.</summary>
public sealed record McpFeedbackMetrics(
    Guid TenantId,
    int TotalFeedbacks,
    int HelpfulCount,
    int NotHelpfulCount,
    double HelpfulPercent,
    IReadOnlyDictionary<McpRiskLevel, int> HelpfulByRiskLevel,
    IReadOnlyDictionary<McpRiskLevel, int> NotHelpfulByRiskLevel);

/// <summary>Resumen del estado de revisiones HITL del tenant.</summary>
public sealed record McpHitlSummary(
    Guid TenantId,
    int OpenCount,
    int InProgressCount,
    int ApprovedCount,
    int RejectedCount)
{
    public int TotalCount => OpenCount + InProgressCount + ApprovedCount + RejectedCount;
    public int PendingCount => OpenCount + InProgressCount;
    public int ResolvedCount => ApprovedCount + RejectedCount;
}
