using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Domain;

namespace Evidata.Modules.Security.Application.Queries;

public record GetUserRolesQuery(Guid UserId, Guid TenantId);

public class GetUserRolesQueryHandler
{
    private readonly IUserRoleAssignmentRepository _assignments;
    private readonly IRoleRepository _roles;

    public GetUserRolesQueryHandler(IUserRoleAssignmentRepository assignments, IRoleRepository roles)
    {
        _assignments = assignments;
        _roles = roles;
    }

    public async Task<IReadOnlyList<UserRoleAssignmentDto>> HandleAsync(GetUserRolesQuery query, CancellationToken ct = default)
    {
        var assignments = await _assignments.GetByUserAsync(query.UserId, query.TenantId, ct);
        var result = new List<UserRoleAssignmentDto>();

        foreach (var a in assignments)
        {
            var role = await _roles.GetByIdAsync(a.RoleId, ct);
            if (role is not null)
                result.Add(new UserRoleAssignmentDto(a.UserId, a.RoleId, role.Name, a.TenantId, a.AssignedAt));
        }

        return result;
    }
}
