using Evidata.Modules.Security.Application.Commands;
using Evidata.Modules.Security.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Security.Api;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly AssignRoleToUserCommandHandler _assign;
    private readonly RemoveRoleFromUserCommandHandler _remove;
    private readonly GetUserRolesQueryHandler _getRoles;

    public RolesController(
        AssignRoleToUserCommandHandler assign,
        RemoveRoleFromUserCommandHandler remove,
        GetUserRolesQueryHandler getRoles)
    {
        _assign = assign;
        _remove = remove;
        _getRoles = getRoles;
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserRoles(Guid userId, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        var result = await _getRoles.HandleAsync(new GetUserRolesQuery(userId, tenantId), ct);
        return Ok(result);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleToUserCommand command, CancellationToken ct)
    {
        var result = await _assign.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleFromUserCommand command, CancellationToken ct)
    {
        await _remove.HandleAsync(command, ct);
        return NoContent();
    }
}
