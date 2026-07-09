using Evidata.Modules.Security.Domain;

namespace Evidata.Modules.Security.Application.Commands;

public record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId, Guid TenantId);

public class RemoveRoleFromUserCommandHandler
{
    private readonly IUserRoleAssignmentRepository _assignments;

    public RemoveRoleFromUserCommandHandler(IUserRoleAssignmentRepository assignments)
    {
        _assignments = assignments;
    }

    public async Task HandleAsync(RemoveRoleFromUserCommand command, CancellationToken ct = default)
    {
        await _assignments.RemoveAsync(command.UserId, command.RoleId, command.TenantId, ct);
    }
}
