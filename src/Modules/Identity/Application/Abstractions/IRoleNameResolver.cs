namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Service for resolving role IDs to role names and vice versa.
/// Provides inter-module integration with the Security module's RBAC system.
/// </summary>
public interface IRoleNameResolver
{
    /// <summary>
    /// Gets the name of a role by its ID.
    /// </summary>
    Task<string?> GetRoleNameAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets the ID of a role by its name.
    /// </summary>
    Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Gets all role names for a collection of role IDs.
    /// </summary>
    Task<IReadOnlyList<string>> GetRoleNamesAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all role IDs for a collection of role names.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetRoleIdsByNamesAsync(IEnumerable<string> roleNames, CancellationToken ct = default);
}
