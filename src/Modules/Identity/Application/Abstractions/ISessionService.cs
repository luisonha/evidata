using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Application service for managing server-side sessions.
/// Sessions are stored in the database and referenced by an opaque cookie.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for a user and returns the opaque session ID to use in the cookie.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="roleIds">Collection of role IDs to snapshot in the session</param>
    /// <param name="permissionsVersion">Current permissions version</param>
    /// <param name="expiryDuration">Session expiry duration (default 24 hours)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Opaque session ID to be stored in the cookie</returns>
    Task<string> CreateSessionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<Guid> roleIds,
        int permissionsVersion = 1,
        TimeSpan? expiryDuration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves an active session by its ID.
    /// Returns null if the session doesn't exist, has expired, or has been revoked.
    /// Updates the session's LastAccessedAt timestamp (sliding expiration).
    /// </summary>
    /// <param name="sessionId">Opaque session ID from cookie</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session object if active, null if not found, expired, or revoked</returns>
    Task<Session?> GetActiveSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes a single session (used on logout).
    /// </summary>
    /// <param name="sessionId">Opaque session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if revoked, false if not found</returns>
    Task<bool> RevokeSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes all sessions for a user in a tenant.
    /// Used when a user is suspended, disabled, or all sessions are explicitly revoked.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> RevokeAllSessionsForUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);
}
