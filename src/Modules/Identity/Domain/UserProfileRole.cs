using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Represents the assignment of a role from the Security module to a user profile within a tenant.
/// This is a bridge table that implements the many-to-many relationship between UserProfile and Role (from Security module).
/// </summary>
public class UserProfileRole : ITenantScoped
{
    /// <summary>
    /// The user profile ID.
    /// </summary>
    public Guid UserProfileId { get; private set; }

    /// <summary>
    /// The role ID from the Security module.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// The tenant ID to ensure tenant isolation.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The date and time when this role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    private UserProfileRole() { }

    /// <summary>
    /// Creates a new user-role assignment.
    /// </summary>
    public static UserProfileRole Create(Guid userProfileId, Guid roleId, Guid tenantId)
    {
        return new UserProfileRole
        {
            UserProfileId = userProfileId,
            RoleId = roleId,
            TenantId = tenantId,
            AssignedAt = DateTime.UtcNow
        };
    }
}
