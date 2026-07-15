using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Represents a server-side session for an authenticated user.
/// Sessions are stored in the database and referenced by an opaque cookie.
/// </summary>
public class Session : ITenantScoped
{
    /// <summary>
    /// Cryptographically random opaque identifier used in the __Host-evidata.sid cookie.
    /// Generated with a secure RNG (not sequential GUID).
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// The tenant this session belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The user this session was created for.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Comma-separated list of role IDs assigned to the user at session creation time.
    /// Stored as a snapshot to avoid recalculating permissions on every request.
    /// Must be invalidated if user roles change while session is active.
    /// </summary>
    public string RolesSnapshot { get; private set; } = default!;

    /// <summary>
    /// Permissions version at the time of session creation.
    /// Can be used to detect if permissions schema has changed and session needs refresh.
    /// </summary>
    public int PermissionsVersion { get; private set; }

    /// <summary>
    /// When the session was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the session will expire if not accessed (UTC).
    /// Updated on each request to implement sliding expiration.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// The last time this session was accessed (UTC).
    /// Updated on each request, used for tracking session activity.
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// If set, the session has been explicitly revoked and is no longer valid.
    /// Set during logout, suspend, disable, or revoke-all-sessions operations.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    private Session() { }

    /// <summary>
    /// Creates a new active session for a user.
    /// </summary>
    /// <param name="sessionId">Opaque session ID (generated with secure RNG)</param>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="rolesSnapshot">Comma-separated list of role IDs at session creation</param>
    /// <param name="permissionsVersion">Current permissions version</param>
    /// <param name="expiryDuration">How long until session expires (default 24 hours)</param>
    /// <returns>New Session instance</returns>
    public static Session Create(
        string sessionId,
        Guid userId,
        Guid tenantId,
        string rolesSnapshot,
        int permissionsVersion,
        TimeSpan? expiryDuration = null)
    {
        expiryDuration ??= TimeSpan.FromHours(24);
        var now = DateTime.UtcNow;

        return new Session
        {
            Id = sessionId,
            UserId = userId,
            TenantId = tenantId,
            RolesSnapshot = rolesSnapshot,
            PermissionsVersion = permissionsVersion,
            CreatedAt = now,
            ExpiresAt = now.Add(expiryDuration.Value),
            LastAccessedAt = now,
            RevokedAt = null
        };
    }

    /// <summary>
    /// Marks the session as revoked (can no longer be used).
    /// </summary>
    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this session is currently active (not revoked and not expired).
    /// </summary>
    public bool IsActive =>
        RevokedAt is null && DateTime.UtcNow <= ExpiresAt;

    /// <summary>
    /// Updates the LastAccessedAt timestamp and extends expiration (sliding window).
    /// </summary>
    /// <param name="slideExpiryBy">Duration to extend expiration (default 24 hours)</param>
    public void Touch(TimeSpan? slideExpiryBy = null)
    {
        if (!IsActive)
            return;

        slideExpiryBy ??= TimeSpan.FromHours(24);
        LastAccessedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(slideExpiryBy.Value);
    }

    /// <summary>
    /// Checks if permissions have changed since session creation.
    /// Caller should compare currentPermissionsVersion with this session's PermissionsVersion.
    /// </summary>
    public bool PermissionsHaveChanged(int currentPermissionsVersion) =>
        PermissionsVersion != currentPermissionsVersion;
}
