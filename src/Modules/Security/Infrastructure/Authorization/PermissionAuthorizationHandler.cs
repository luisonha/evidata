using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that validates if the current user has a specific permission
/// in the current tenant. This is the critical path for protecting admin and fine-grained
/// operations with dynamic permission checks.
/// 
/// Rationale:
/// - ICurrentUserContext provides UserId and TenantId from the authenticated context
/// - Queries the real RBAC tables (UserRoleAssignments → RolePermissions → Permissions)
///   to fetch the user's permissions
/// - Checks if the user has the requested permission code (e.g., "Admin.ReadUsers")
/// - If the user lacks the permission, the requirement fails (403 Forbidden at API boundary)
/// - Cross-tenant isolation is guaranteed: if a user of tenant A requests with TenantId=B,
///   they will have no roles in tenant B, so no permissions in tenant B, and the check fails
/// - Fail-closed: if not authenticated, explicit fail() is called
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserContext _currentUser;
    private readonly SecurityDbContext _securityDbContext;

    public PermissionAuthorizationHandler(
        ICurrentUserContext currentUser,
        SecurityDbContext securityDbContext)
    {
        _currentUser = currentUser;
        _securityDbContext = securityDbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        // Query user's permissions in the current tenant via roles.
        // This uses the real RBAC tables: UserRoleAssignments → RolePermissions → Permissions
        var userPermissions = await _securityDbContext.UserRoleAssignments
            .Where(a => a.UserId == _currentUser.UserId && a.TenantId == _currentUser.TenantId)
            .Join(_securityDbContext.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_securityDbContext.Permissions, id => id, p => p.Id, (id, p) => p.Name)
            .Distinct()
            .ToListAsync();

        // Check if user has the required permission.
        if (userPermissions.Contains(requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    // For testing purposes: allows tests to invoke the authorization logic directly
    public async Task<bool> EvaluateAsync(PermissionRequirement requirement)
    {
        var context = new AuthorizationHandlerContext(new[] { requirement }, null!, null);
        await HandleRequirementAsync(context, requirement);
        return context.HasSucceeded;
    }
}
