using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Evidata.Modules.Security.Infrastructure.Authorization;

/// <summary>
/// Custom authorization policy provider that dynamically constructs policies for permission-based authorization.
/// 
/// Recognizes policy names with the "HasPermission:" prefix and constructs an AuthorizationPolicy
/// with a PermissionRequirement for the permission code after the prefix.
/// 
/// Examples:
///   - "HasPermission:Admin.ReadUsers" → creates a policy that requires the "Admin.ReadUsers" permission
///   - "HasPermission:Admin.ManageUsers" → creates a policy that requires the "Admin.ManageUsers" permission
/// 
/// For all other policy names (including "TenantOwnerOrComplianceAdmin" and the default FallbackPolicy),
/// delegates to DefaultAuthorizationPolicyProvider to maintain backward compatibility.
/// 
/// Rationale:
/// - Permission codes are stored in the database and may be added/removed without code changes
/// - Dynamic policy construction avoids hard-coding all permissions in HostBuilderFactory
/// - Failing to construct a policy (e.g., malformed name) returns null, letting ASP.NET handle gracefully
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPolicyPrefix = "HasPermission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // If the policy name starts with "HasPermission:", extract the permission code
        // and construct a policy dynamically
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            var permissionCode = policyName.Substring(PermissionPolicyPrefix.Length);
            
            // Build a policy that requires the extracted permission
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // For all other policies, delegate to the default provider
        // This maintains backward compatibility with "TenantOwnerOrComplianceAdmin" and the FallbackPolicy
        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallback.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallback.GetFallbackPolicyAsync();
    }
}
