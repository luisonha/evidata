using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Domain;

namespace Evidata.Modules.Security.Application.Queries;

/// <summary>
/// Query to retrieve all available roles from the RBAC catalog.
/// </summary>
public record GetAllRolesQuery;

/// <summary>
/// Retrieves all roles with id, name, and description.
/// Used by admin endpoints to list available roles for role assignment operations.
/// </summary>
public class GetAllRolesQueryHandler
{
    private readonly IRoleRepository _roles;

    public GetAllRolesQueryHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyList<RoleCatalogDto>> HandleAsync(CancellationToken ct = default)
    {
        var allRoles = await _roles.GetAllAsync(ct);
        return allRoles
            .Select(r => new RoleCatalogDto(r.Id, r.Name, r.Description))
            .ToList()
            .AsReadOnly();
    }
}
