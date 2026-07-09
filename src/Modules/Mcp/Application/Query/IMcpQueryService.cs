namespace Evidata.Modules.Mcp.Application.Query;

/// <summary>
/// Servicio de consulta MCP con contexto RAT integrado.
/// Orquesta: clasificación de riesgo → enriquecimiento con RATs → respuesta → registro.
/// </summary>
public interface IMcpQueryService
{
    /// <summary>
    /// Procesa una consulta de cumplimiento y retorna una respuesta enriquecida
    /// con el contexto RAT del tenant.
    /// </summary>
    Task<McpQueryResponse> QueryAsync(McpQueryRequest request, CancellationToken ct = default);
}
