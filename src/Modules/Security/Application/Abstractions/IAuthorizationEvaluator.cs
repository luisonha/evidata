namespace Evidata.Modules.Security.Application.Abstractions;

/// <summary>
/// Evalúa si el usuario actual tiene permiso para ejecutar una acción sobre un recurso.
/// </summary>
public interface IAuthorizationEvaluator
{
    /// <summary>Retorna true si el usuario tiene el permiso "resource:action" en su tenant.</summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string resource, string action, CancellationToken ct = default);

    /// <summary>Retorna todos los permisos del usuario en el tenant.</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
}
