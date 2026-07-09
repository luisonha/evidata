using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Domain;

namespace Evidata.Modules.Security.Application.Commands;

public record AssignRoleToUserCommand(Guid UserId, Guid RoleId, Guid TenantId);

public class AssignRoleToUserCommandHandler
{
    private readonly IUserRoleAssignmentRepository _assignments;
    private readonly IRoleRepository _roles;

    public AssignRoleToUserCommandHandler(IUserRoleAssignmentRepository assignments, IRoleRepository roles)
    {
        _assignments = assignments;
        _roles = roles;
    }

    public async Task<UserRoleAssignmentDto> HandleAsync(AssignRoleToUserCommand command, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdAsync(command.RoleId, ct)
            ?? throw new InvalidOperationException($"Role {command.RoleId} not found.");

        var existing = await _assignments.GetByUserAsync(command.UserId, command.TenantId, ct);
        var alreadyAssigned = existing.FirstOrDefault(a => a.RoleId == command.RoleId);
        if (alreadyAssigned is not null)
            return new UserRoleAssignmentDto(command.UserId, command.RoleId, role.Name, command.TenantId, alreadyAssigned.AssignedAt);

        var assignment = UserRoleAssignment.Create(command.UserId, command.RoleId, command.TenantId);
        await _assignments.AssignAsync(assignment, ct);

        return new UserRoleAssignmentDto(command.UserId, command.RoleId, role.Name, command.TenantId, assignment.AssignedAt);
    }
}
