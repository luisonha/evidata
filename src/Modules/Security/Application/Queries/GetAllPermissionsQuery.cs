using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Domain;

namespace Evidata.Modules.Security.Application.Queries;

/// <summary>
/// Query to retrieve all available permissions from the RBAC catalog.
/// </summary>
public record GetAllPermissionsQuery;

/// <summary>
/// Retrieves all permissions with id, name, resource, action, and description.
/// Used by admin endpoints to list available permissions in the system.
/// Permissions are derived from the RBAC catalog and are role-agnostic.
/// </summary>
public class GetAllPermissionsQueryHandler
{
    private readonly IPermissionRepository _permissions;

    public GetAllPermissionsQueryHandler(IPermissionRepository permissions)
    {
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<PermissionDto>> HandleAsync(CancellationToken ct = default)
    {
        var allPermissions = await _permissions.GetAllAsync(ct);
        return allPermissions
            .Select(p => new PermissionDto(p.Id, p.Name, p.Resource, p.Action, p.Description))
            .ToList()
            .AsReadOnly();
    }
}
