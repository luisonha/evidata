using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// ICurrentUserContext implementation for session-based authentication.
/// Reads the opaque __Host-evidata.sid cookie and resolves the active session from the database.
/// </summary>
public sealed class SessionCurrentUserContext : ICurrentUserContext
{
    private readonly Guid _userId;
    private readonly Guid _tenantId;
    private readonly string _email = string.Empty;
    private readonly bool _isAuthenticated;
    private readonly Session? _session;

    public Guid UserId => _userId;
    public Guid TenantId => _tenantId;
    public string Email => _email;
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Gets the underlying session if authenticated, null otherwise.
    /// Can be used by authorization handlers to validate permissions version, etc.
    /// </summary>
    public Session? Session => _session;

    private SessionCurrentUserContext(Session? session, Guid userId, Guid tenantId, string email, bool isAuthenticated)
    {
        _session = session;
        _userId = userId;
        _tenantId = tenantId;
        _email = email;
        _isAuthenticated = isAuthenticated;
    }

    /// <summary>
    /// Factory method to create context from an active session.
    /// Called from middleware after retrieving session from database.
    /// </summary>
    public static SessionCurrentUserContext FromSession(Session session, string email)
    {
        return new SessionCurrentUserContext(
            session,
            session.UserId,
            session.TenantId,
            email,
            isAuthenticated: true);
    }

    /// <summary>
    /// Factory method to create unauthenticated context.
    /// </summary>
    public static SessionCurrentUserContext CreateUnauthenticated()
    {
        return new SessionCurrentUserContext(
            session: null,
            userId: Guid.Empty,
            tenantId: Guid.Empty,
            email: string.Empty,
            isAuthenticated: false);
    }
}
