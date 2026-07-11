using Microsoft.AspNetCore.Authorization;

namespace Evidata.Modules.Security.Infrastructure.Authorization;

/// <summary>
/// Requirement that validates the current user has either TenantOwner or ComplianceAdmin role
/// in the requested tenant. Used to restrict sensitive operations like role assignment/removal
/// to admin-level users only.
/// </summary>
public class TenantOwnerOrComplianceAdminRequirement : IAuthorizationRequirement
{
}
