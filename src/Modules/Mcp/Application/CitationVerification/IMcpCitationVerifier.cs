using Evidata.Modules.Mcp.Domain;

namespace Evidata.Modules.Mcp.Application.CitationVerification;

/// <summary>
/// Verifica que las fuentes citadas en respuestas MCP existan
/// y sean accesibles para el tenant que las generó.
/// </summary>
public interface IMcpCitationVerifier
{
    /// <summary>
    /// Verifica una lista de citaciones contra las fuentes de datos del tenant.
    /// Retorna un reporte con el estado de cada citación.
    /// </summary>
    Task<CitationVerificationReport> VerifyAsync(
        Guid interactionId,
        Guid tenantId,
        IReadOnlyList<McpCitation> citations,
        CancellationToken ct = default);
}
