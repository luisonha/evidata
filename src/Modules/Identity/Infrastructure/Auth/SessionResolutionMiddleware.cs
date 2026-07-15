using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Middleware that resolves the server-side session from the __Host-evidata.sid cookie
/// and populates ICurrentUserContext for downstream handlers.
///
/// This middleware:
/// 1. Reads the opaque session ID from the __Host-evidata.sid cookie (HttpOnly, Secure, SameSite=Strict)
/// 2. Looks up the session in the database
/// 3. If valid and not expired/revoked, loads user profile data and creates SessionCurrentUserContext
/// 4. Populates HttpContext.Items with the context for DI resolution
/// 5. Allows logout/suspend/disable to immediately revoke sessions (no TTL waiting)
/// </summary>
public class SessionResolutionMiddleware
{
    private const string SessionCookieName = "__Host-evidata.sid";
    private const string CurrentUserContextKey = "CurrentUserContext";

    private readonly RequestDelegate _next;
    private readonly ILogger<SessionResolutionMiddleware> _logger;

    public SessionResolutionMiddleware(RequestDelegate next, ILogger<SessionResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IdentityDbContext dbContext,
        ISessionService sessionService)
    {
        SessionCurrentUserContext currentUserContext;

        // Try to read session ID from cookie
        if (httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
        {
            // Attempt to load active session from database
            var session = await sessionService.GetActiveSessionAsync(sessionId);

            if (session != null)
            {
                // Load user profile to get email and other info
                var userProfile = await dbContext.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == session.UserId && u.TenantId == session.TenantId);

                if (userProfile != null)
                {
                    // Create authenticated context
                    currentUserContext = SessionCurrentUserContext.FromSession(session, userProfile.Email);
                    _logger.LogDebug("Session {SessionId} resolved for user {UserId} in tenant {TenantId}",
                        sessionId, session.UserId, session.TenantId);
                }
                else
                {
                    // Session exists but user no longer exists (shouldn't happen in normal flow)
                    _logger.LogWarning("Session {SessionId} found but user {UserId} no longer exists", sessionId, session.UserId);
                    currentUserContext = SessionCurrentUserContext.CreateUnauthenticated();
                }
            }
            else
            {
                // Session invalid, expired, or revoked
                _logger.LogDebug("Session {SessionId} not found, expired, or revoked", sessionId);
                currentUserContext = SessionCurrentUserContext.CreateUnauthenticated();

                // Clear the cookie to avoid repeated lookup attempts
                httpContext.Response.Cookies.Delete(SessionCookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
            }
        }
        else
        {
            // No session cookie present
            currentUserContext = SessionCurrentUserContext.CreateUnauthenticated();
        }

        // Store in HttpContext.Items for DI container to inject
        httpContext.Items[CurrentUserContextKey] = currentUserContext;

        await _next(httpContext);
    }
}

/// <summary>
/// Factory service that provides SessionCurrentUserContext from HttpContext.Items.
/// Registered in DI as ICurrentUserContext for compatibility with existing code.
/// </summary>
public class SessionResolverFactory : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionResolverFactory(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private SessionCurrentUserContext? GetContextFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("CurrentUserContext", out var contextObj) == true
            && contextObj is SessionCurrentUserContext context)
        {
            return context;
        }
        return null;
    }

    public Guid UserId
    {
        get
        {
            var context = GetContextFromHttpContext();
            return context?.UserId ?? Guid.Empty;
        }
    }

    public Guid TenantId
    {
        get
        {
            var context = GetContextFromHttpContext();
            return context?.TenantId ?? Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            var context = GetContextFromHttpContext();
            return context?.Email ?? string.Empty;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var context = GetContextFromHttpContext();
            return context?.IsAuthenticated ?? false;
        }
    }
}
