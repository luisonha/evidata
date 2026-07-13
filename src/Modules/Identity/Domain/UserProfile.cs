using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Domain;

public class UserProfile : ITenantScoped
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public Guid TenantId { get; private set; }
    
    /// <summary>
    /// The current lifecycle status of this user.
    /// </summary>
    public UserStatus Status { get; private set; }
    
    /// <summary>
    /// The last time this user successfully logged in (UTC).
    /// Updated on each successful Entra ID callback/session creation.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Navigation property for the many-to-many relationship with roles.
    /// </summary>
    private readonly List<UserProfileRole> _userProfileRoles = [];
    public IReadOnlyList<UserProfileRole> UserProfileRoles => _userProfileRoles.AsReadOnly();

    private UserProfile() { }

    public static UserProfile Create(string externalId, string provider, string email, string displayName, Guid tenantId)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Provider = provider,
            Email = email,
            DisplayName = displayName,
            TenantId = tenantId,
            Status = UserStatus.Invited,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user (transitions from Invited to Active).
    /// </summary>
    public void Activate()
    {
        if (Status != UserStatus.Invited)
        {
            throw new InvalidOperationException($"Cannot activate a user with status {Status}");
        }
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the user's login timestamp.
    /// Called after successful authentication via Entra ID or other providers.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links the user to an Entra AD external identity (if not already linked).
    /// Called during first successful Entra ID login.
    /// </summary>
    public void LinkEntraId(string entraOid)
    {
        if (string.IsNullOrWhiteSpace(entraOid))
            throw new ArgumentNullException(nameof(entraOid));

        // Only link if not already linked
        if (Provider != "EntraId" || ExternalId != entraOid)
        {
            Provider = "EntraId";
            ExternalId = entraOid;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Suspends the user (temporary deactivation).
    /// </summary>
    public void Suspend()
    {
        if (Status != UserStatus.Active)
        {
            throw new InvalidOperationException($"Cannot suspend a user with status {Status}");
        }
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the user (permanent deactivation during Sprint 3).
    /// </summary>
    public void Disable()
    {
        if (Status != UserStatus.Active && Status != UserStatus.Suspended)
        {
            throw new InvalidOperationException($"Cannot disable a user with status {Status}");
        }
        Status = UserStatus.Disabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the user (transitions from Suspended or Disabled to Active).
    /// </summary>
    public void Reactivate()
    {
        if (Status != UserStatus.Suspended && Status != UserStatus.Disabled)
        {
            throw new InvalidOperationException($"Cannot reactivate a user with status {Status}");
        }
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the invitation as revoked (user cannot reactivate this account).
    /// </summary>
    public void RevokeInvitation()
    {
        if (Status != UserStatus.Invited)
        {
            throw new InvalidOperationException($"Cannot revoke invitation for a user with status {Status}");
        }
        Status = UserStatus.InvitationRevoked;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a role to this user (adds to many-to-many relationship).
    /// </summary>
    public void AssignRole(Guid roleId)
    {
        if (_userProfileRoles.Any(r => r.RoleId == roleId))
        {
            return; // Role already assigned
        }
        _userProfileRoles.Add(UserProfileRole.Create(Id, roleId, TenantId));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a role from this user.
    /// </summary>
    public void RemoveRole(Guid roleId)
    {
        var roleAssignment = _userProfileRoles.FirstOrDefault(r => r.RoleId == roleId);
        if (roleAssignment is not null)
        {
            _userProfileRoles.Remove(roleAssignment);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Replaces the user's roles with a new set (for many-to-many management).
    /// </summary>
    public void SetRoles(IEnumerable<Guid> roleIds)
    {
        _userProfileRoles.Clear();
        foreach (var roleId in roleIds)
        {
            _userProfileRoles.Add(UserProfileRole.Create(Id, roleId, TenantId));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the role IDs assigned to this user.
    /// </summary>
    public IEnumerable<Guid> GetRoleIds() => _userProfileRoles.Select(r => r.RoleId);

    /// <summary>
    /// Checks if the user is active.
    /// Kept for backward compatibility during transition.
    /// </summary>
    public bool IsActive => Status == UserStatus.Active;

    /// <summary>
    /// Checks if the user is in any of the given roles.
    /// </summary>
    public bool HasRole(Guid roleId) => _userProfileRoles.Any(r => r.RoleId == roleId);

    /// <summary>
    /// Checks if the user has any role in a collection.
    /// </summary>
    public bool HasAnyRole(IEnumerable<Guid> roleIds) => _userProfileRoles.Any(r => roleIds.Contains(r.RoleId));
}
