using Evidata.Modules.Mcp.Domain;

namespace Evidata.Modules.Mcp.Application.Abstractions;

/// <summary>
/// Servicio de registro y consulta de interacciones MCP.
/// </summary>
public interface IMcpInteractionService
{
    /// <summary>Registra una interacción exitosa del asistente MCP.</summary>
    Task<McpInteraction> RecordAsync(
        Guid tenantId,
        Guid userId,
        string question,
        string answer,
        McpRiskLevel riskLevel,
        bool usedTenantContext,
        bool requiresHumanReview,
        CancellationToken ct = default);

    /// <summary>Registra una interacción fallida.</summary>
    Task<McpInteraction> RecordFailedAsync(
        Guid tenantId,
        Guid userId,
        string question,
        CancellationToken ct = default);

    /// <summary>Agrega una citación a una interacción existente.</summary>
    Task<McpCitation> AddCitationAsync(
        Guid interactionId,
        McpCitationSourceType sourceType,
        string sourceId,
        string fragment,
        string? sourceVersion = null,
        CancellationToken ct = default);

    /// <summary>Escala la interacción a revisión humana (HITL).</summary>
    Task RequestHumanReviewAsync(Guid interactionId, CancellationToken ct = default);

    /// <summary>Registra feedback de un usuario sobre una interacción.</summary>
    Task RecordFeedbackAsync(
        Guid interactionId,
        Guid tenantId,
        Guid userId,
        McpFeedbackRating rating,
        string? comment,
        CancellationToken ct = default);

    /// <summary>Obtiene una interacción por Id (incluye citaciones).</summary>
    Task<McpInteraction?> GetByIdAsync(Guid interactionId, CancellationToken ct = default);

    /// <summary>Lista interacciones recientes de un tenant.</summary>
    Task<IReadOnlyList<McpInteraction>> GetRecentByTenantAsync(
        Guid tenantId, int limit = 20, CancellationToken ct = default);

    /// <summary>Lista interacciones pendientes de revisión humana.</summary>
    Task<IReadOnlyList<McpInteraction>> GetPendingReviewAsync(
        Guid tenantId, CancellationToken ct = default);
}
