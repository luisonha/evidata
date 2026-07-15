using Microsoft.AspNetCore.Authorization;

namespace Evidata.Modules.Security.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that validates the current user has a specific permission
/// in the current tenant. Used for dynamic policy-based authorization via attributes like:
/// [Authorize(Policy = "HasPermission:Admin.ReadUsers")]
/// 
/// The permission code is extracted from the policy name (e.g., "HasPermission:Admin.ReadUsers" → "Admin.ReadUsers")
/// and validated against the user's roles and their associated permissions in SecurityDbContext.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The permission code that the user must have (e.g., "Admin.ReadUsers", "Admin.ManageUsers")
    /// </summary>
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode ?? throw new ArgumentNullException(nameof(permissionCode));
    }
}
