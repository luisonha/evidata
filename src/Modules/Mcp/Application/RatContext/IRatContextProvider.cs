namespace Evidata.Modules.Mcp.Application.RatContext;

/// <summary>
/// Proveedor de contexto RAT para el módulo MCP.
/// Retorna una proyección mínima de los tratamientos aprobados del tenant.
/// </summary>
public interface IRatContextProvider
{
    /// <summary>
    /// Obtiene el snapshot RAT del tenant con tratamientos aprobados.
    /// Retorna un snapshot vacío si el tenant no tiene RATs aprobados.
    /// </summary>
    Task<RatContextSnapshot> GetSnapshotAsync(Guid tenantId, CancellationToken ct = default);
}
