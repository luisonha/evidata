using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Default implementation of ISessionService.
/// Manages server-side sessions stored in PostgreSQL.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IdentityDbContext _context;
    private const int OpaqueIdLengthBytes = 32;  // 256 bits of entropy -> ~43 chars base64

    public SessionService(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<string> CreateSessionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<Guid> roleIds,
        int permissionsVersion = 1,
        TimeSpan? expiryDuration = null,
        CancellationToken ct = default)
    {
        // Generate cryptographically secure opaque session ID
        var sessionId = GenerateOpaqueSessionId();

        // Snapshot roles as comma-separated string
        var rolesSnapshot = string.Join(",", roleIds);

        // Create domain entity
        var session = Session.Create(
            sessionId,
            userId,
            tenantId,
            rolesSnapshot,
            permissionsVersion,
            expiryDuration);

        // Persist
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(ct);

        return sessionId;
    }

    /// <inheritdoc/>
    public async Task<Session?> GetActiveSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        // Retrieve from database
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken: ct);

        if (session is null)
            return null;

        // Check if still active (not revoked and not expired)
        if (!session.IsActive)
            return null;

        // Update LastAccessedAt and extend expiration (sliding window)
        session.Touch();
        await _context.SaveChangesAsync(ct);

        return session;
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken: ct);

        if (session is null)
            return false;

        session.Revoke();
        await _context.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> RevokeAllSessionsForUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // Find all active sessions for this user in this tenant
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && s.TenantId == tenantId && s.RevokedAt == null)
            .ToListAsync(cancellationToken: ct);

        if (sessions.Count == 0)
            return 0;

        // Revoke each one
        foreach (var session in sessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync(ct);
        return sessions.Count;
    }

    /// <summary>
    /// Generates a cryptographically secure opaque session ID.
    /// </summary>
    private static string GenerateOpaqueSessionId()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[OpaqueIdLengthBytes];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
