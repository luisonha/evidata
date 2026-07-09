using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that validates if the current user has either TenantOwner or ComplianceAdmin
/// role in the current tenant. This is the critical path for protecting role assignment/removal operations.
/// 
/// Rationale:
/// - ICurrentUserContext provides UserId and TenantId from the authenticated context
/// - IAuthorizationEvaluator queries the real RBAC tables to fetch user roles
/// - We check for role names "TenantOwner" or "ComplianceAdmin" (seeded in SecurityDbContext)
/// - If the user lacks these roles, the requirement fails (403 Forbidden at API boundary)
/// - Cross-tenant isolation is guaranteed: if a TenantOwner of tenant A requests with TenantId=B,
///   they will have no roles in tenant B, so the check will fail
/// </summary>
public class TenantOwnerOrComplianceAdminHandler : AuthorizationHandler<TenantOwnerOrComplianceAdminRequirement>
{
    private readonly ICurrentUserContext _currentUser;
    private readonly SecurityDbContext _securityDbContext;

    public TenantOwnerOrComplianceAdminHandler(
        ICurrentUserContext currentUser,
        SecurityDbContext securityDbContext)
    {
        _currentUser = currentUser;
        _securityDbContext = securityDbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantOwnerOrComplianceAdminRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        // Query user's roles in the current tenant.
        // This uses the real RBAC tables, never mocked or hardcoded.
        var userRoles = await _securityDbContext.UserRoleAssignments
            .Where(a => a.UserId == _currentUser.UserId && a.TenantId == _currentUser.TenantId)
            .Join(_securityDbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name)
            .Distinct()
            .ToListAsync();

        // Check if user has one of the allowed roles.
        if (userRoles.Contains("TenantOwner") || userRoles.Contains("ComplianceAdmin"))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    // For testing purposes: allows tests to invoke the authorization logic directly
    public async Task<bool> EvaluateAsync(TenantOwnerOrComplianceAdminRequirement requirement)
    {
        var context = new AuthorizationHandlerContext(new[] { requirement }, null!, null);
        await HandleRequirementAsync(context, requirement);
        return context.HasSucceeded;
    }
}

