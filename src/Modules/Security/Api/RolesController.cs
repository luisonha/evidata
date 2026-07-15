using Evidata.Modules.Security.Application.Commands;
using Evidata.Modules.Security.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Security.Api;

[ApiController]
[Authorize]
[Route("api/roles")]
[Route("api/v1/admin/roles")]
public class RolesController : ControllerBase
{
    private readonly AssignRoleToUserCommandHandler _assign;
    private readonly RemoveRoleFromUserCommandHandler _remove;
    private readonly GetUserRolesQueryHandler _getRoles;
    private readonly GetAllRolesQueryHandler _getAllRoles;
    private readonly GetAllPermissionsQueryHandler _getAllPermissions;

    public RolesController(
        AssignRoleToUserCommandHandler assign,
        RemoveRoleFromUserCommandHandler remove,
        GetUserRolesQueryHandler getRoles,
        GetAllRolesQueryHandler getAllRoles,
        GetAllPermissionsQueryHandler getAllPermissions)
    {
        _assign = assign;
        _remove = remove;
        _getRoles = getRoles;
        _getAllRoles = getAllRoles;
        _getAllPermissions = getAllPermissions;
    }

    /// <summary>
    /// List all available roles from the RBAC catalog.
    /// Only TenantOwner or ComplianceAdmin can access this endpoint.
    /// Returns roles with id, name, and description.
    /// Route: GET /api/v1/admin/roles or GET /api/roles
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> ListAllRoles(CancellationToken ct)
    {
        var result = await _getAllRoles.HandleAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// List all available permissions from the RBAC catalog.
    /// Only TenantOwner or ComplianceAdmin can access this endpoint.
    /// Returns permissions with id, name, resource, action, and description.
    /// Route: GET /api/v1/admin/roles/permissions or GET /api/roles/permissions
    /// Note: distinct from GET /api/permissions (AuthController), which returns the
    /// authenticated user's own effective permissions, not the full catalog.
    /// </summary>
    [HttpGet("permissions")]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> ListAllPermissions(CancellationToken ct)
    {
        var result = await _getAllPermissions.HandleAsync(ct);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserRoles(Guid userId, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        var result = await _getRoles.HandleAsync(new GetUserRolesQuery(userId, tenantId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Assigns a role to a user in the current tenant.
    /// Only TenantOwner or ComplianceAdmin can perform this operation.
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleToUserCommand command, CancellationToken ct)
    {
        var result = await _assign.HandleAsync(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Removes a role from a user in the current tenant.
    /// Only TenantOwner or ComplianceAdmin can perform this operation.
    /// </summary>
    [HttpDelete("remove")]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleFromUserCommand command, CancellationToken ct)
    {
        await _remove.HandleAsync(command, ct);
        return NoContent();
    }
}
