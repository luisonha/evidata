namespace Evidata.Modules.Mcp.Application.Audit;

/// <summary>
/// Servicio de auditoría MCP — historial de interacciones, métricas de feedback
/// y resumen de revisiones HITL. Solo lectura.
/// </summary>
public interface IMcpAuditService
{
    /// <summary>Retorna historial paginado de interacciones del tenant.</summary>
    Task<McpInteractionHistory> GetInteractionHistoryAsync(
        McpAuditQuery query, CancellationToken ct = default);

    /// <summary>Métricas de feedback (Helpful/NotHelpful) por nivel de riesgo.</summary>
    Task<McpFeedbackMetrics> GetFeedbackMetricsAsync(
        Guid tenantId, CancellationToken ct = default);

    /// <summary>Resumen de revisiones HITL (open/in-progress/resolved) del tenant.</summary>
    Task<McpHitlSummary> GetHitlSummaryAsync(
        Guid tenantId, CancellationToken ct = default);
}
