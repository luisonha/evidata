using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Evidata.Modules.Identity.Api;

/// <summary>
/// Authentication endpoints for Entra ID OpenID Connect login flow.
/// Implements GET /auth/login, GET /auth/callback, POST /auth/logout, and session/me/permissions/csrf endpoints.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IOidcService _oidcService;
    private readonly IEntraIdTokenService _entraIdTokenService;
    private readonly IUserResolutionService _userResolutionService;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly CompleteLoginCommandHandler _completeLoginHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly GetSessionStatusQueryHandler _getSessionStatusHandler;
    private readonly GetCurrentUserProfileQueryHandler _getUserProfileHandler;
    private readonly GetCurrentUserPermissionsQueryHandler _getUserPermissionsHandler;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOidcService oidcService,
        IEntraIdTokenService entraIdTokenService,
        IUserResolutionService userResolutionService,
        ISessionService sessionService,
        ICurrentUserContext currentUserContext,
        ITenantRepository tenantRepository,
        CompleteLoginCommandHandler completeLoginHandler,
        LogoutCommandHandler logoutHandler,
        GetSessionStatusQueryHandler getSessionStatusHandler,
        GetCurrentUserProfileQueryHandler getUserProfileHandler,
        GetCurrentUserPermissionsQueryHandler getUserPermissionsHandler,
        IAntiforgery antiforgery,
        ILogger<AuthController> logger)
    {
        _oidcService = oidcService;
        _entraIdTokenService = entraIdTokenService;
        _userResolutionService = userResolutionService;
        _sessionService = sessionService;
        _currentUserContext = currentUserContext;
        _tenantRepository = tenantRepository;
        _completeLoginHandler = completeLoginHandler;
        _logoutHandler = logoutHandler;
        _getSessionStatusHandler = getSessionStatusHandler;
        _getUserProfileHandler = getUserProfileHandler;
        _getUserPermissionsHandler = getUserPermissionsHandler;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    /// <summary>
    /// Initiates OpenID Connect login flow by redirecting to Entra ID.
    /// Requires a tenant slug to identify which tenant the user is logging into.
    /// Generates state and nonce for CSRF/replay protection.
    /// 
    /// Public endpoint — no authentication required.
    /// Query params:
    /// - tenant (required): Slug of the tenant to log into
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromQuery] string? tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenant))
            return BadRequest(new { message = "Tenant slug is required" });

        // Resolve tenant by slug
        var resolvedTenant = await _tenantRepository.GetBySlugAsync(tenant, ct);
        if (resolvedTenant is null)
        {
            _logger.LogWarning("Login attempt with non-existent tenant slug: {Slug}", tenant);
            return BadRequest(new { message = "Tenant not found" });
        }

        if (resolvedTenant.Status != TenantStatus.Active)
        {
            _logger.LogWarning("Login attempt with inactive tenant: {TenantId} (slug: {Slug})", resolvedTenant.Id, tenant);
            return BadRequest(new { message = "Tenant is not active" });
        }

        // Generate state and nonce with tenant context
        var (state, nonce) = await _oidcService.GenerateStateAndNonceAsync(resolvedTenant.Id, ct);

        // Read OpenID Connect configuration
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var redirectUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/auth/callback";

        var oidcSettings = config.GetSection("OIDC");
        var authority = oidcSettings["Authority"] ?? "https://login.microsoftonline.com/common/v2.0";
        var clientId = oidcSettings["ClientId"] ?? throw new InvalidOperationException("OIDC:ClientId not configured");
        var scope = oidcSettings["Scope"] ?? "openid profile email";

        // Build authorization request URL
        var authorizationUrl = $"{authority}/authorize?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"response_type=code&" +
            $"scope={Uri.EscapeDataString(scope)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"state={Uri.EscapeDataString(state)}&" +
            $"nonce={Uri.EscapeDataString(nonce)}&" +
            $"response_mode=form_post";

        _logger.LogInformation("Redirecting to Entra ID for tenant {TenantId}", resolvedTenant.Id);
        return Redirect(authorizationUrl);
    }

    /// <summary>
    /// Callback endpoint for Entra ID authorization response.
    /// Validates state (which includes tenant context and nonce), exchanges authorization code for token,
    /// validates JWT, resolves user, creates session, and returns session info.
    /// 
    /// Public endpoint — no authentication required.
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromForm] string? code,
        [FromForm] string? state,
        [FromForm] string? error,
        [FromForm] string? error_description,
        CancellationToken ct)
    {
        // Validate OIDC flow wasn't denied
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OIDC error: {Error} - {Description}", error, error_description);
            return BadRequest(new { error, error_description });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Callback received without authorization code");
            return BadRequest(new { message = "Missing authorization code" });
        }

        try
        {
            // Validate state (CSRF protection) and recover tenant ID and nonce
            if (string.IsNullOrWhiteSpace(state))
            {
                _logger.LogWarning("Callback received with missing state parameter");
                return BadRequest(new { message = "Invalid or expired state parameter" });
            }

            var stateValidation = await _oidcService.ValidateStateAsync(state, ct);
            if (!stateValidation.IsValid || stateValidation.TenantId is null || string.IsNullOrWhiteSpace(stateValidation.Nonce))
            {
                _logger.LogWarning("Invalid or expired state parameter");
                return BadRequest(new { message = "Invalid or expired state parameter" });
            }

            var tenantId = stateValidation.TenantId.Value;
            var nonce = stateValidation.Nonce;

            // Build the redirect URI for token exchange
            var redirectUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/auth/callback";

            // Step 1: Exchange authorization code for token and validate JWT
            var claims = await _entraIdTokenService.ExchangeCodeForTokenAsync(code, redirectUri, nonce, ct);
            _logger.LogInformation("Token exchanged successfully for user {Oid} in tenant {TenantId}", claims.Oid, tenantId);

            // Step 2: Resolve user from Entra ID claims
            var (user, invitation, errorCode) = await _userResolutionService.ResolveUserAsync(claims, tenantId, ct);

            if (errorCode is not null)
            {
                // Return appropriate error response based on error code
                var statusCode = MapErrorCodeToHttpStatus(errorCode);
                _logger.LogWarning("User resolution failed with error code: {ErrorCode}", errorCode);
                return StatusCode(statusCode, new { errorCode, message = errorCode });
            }

            // If we have an invitation (user not yet in system), create the user first
            if (user is null && invitation is not null)
            {
                user = UserProfile.Create(
                    claims.Oid,
                    "EntraId",
                    claims.Email,
                    claims.DisplayName,
                    tenantId);
                _logger.LogInformation("Created new user from invitation {InvitationId}", invitation.Id);
            }

            if (user is null)
            {
                _logger.LogError("User resolution failed: no user found and no invitation available");
                return BadRequest(new { message = "User resolution failed" });
            }

            // Link Entra OID if not already linked
            if (user.Provider != "EntraId" || user.ExternalId != claims.Oid)
            {
                user.LinkEntraId(claims.Oid);
                _logger.LogInformation("Linked Entra OID to user {UserId}", user.Id);
            }

            // Get user's role IDs for session snapshot
            var roleIds = user.GetRoleIds().ToList();

            // Step 3: Complete login (creates session, activates user if invited, records login time)
            var command = new CompleteLoginCommand(tenantId, user, invitation, roleIds);
            var response = await _completeLoginHandler.HandleAsync(command, ct);
            _logger.LogInformation("Login completed for user {UserId} in tenant {TenantId}", user.Id, tenantId);

            // Set secure session cookie
            Response.Cookies.Append(
                "__Host-evidata.sid",
                response.SessionId,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Path = "/",
                    Expires = response.SessionExpiresAt
                });

            // Return session info for the frontend to handle
            return Ok(response);
        }
        catch (EntraIdTokenExchangeException ex)
        {
            _logger.LogError(ex, "Token exchange failed: {ErrorCode}", ex.ErrorCode);
            return StatusCode(StatusCodes.Status401Unauthorized, 
                new { message = "Token exchange failed", error = ex.ErrorCode, error_description = ex.ErrorDescription });
        }
        catch (JwtValidationException ex)
        {
            _logger.LogError(ex, "JWT validation failed");
            return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Token validation failed" });
        }
        catch (IdentityDomainException ex)
        {
            var statusCode = MapErrorCodeToHttpStatus(ex.ErrorCode);
            _logger.LogError(ex, "Identity domain error: {ErrorCode}", ex.ErrorCode);
            return StatusCode(statusCode, new { errorCode = ex.ErrorCode, message = ex.ErrorMessage ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login callback");
            return StatusCode(500, new { message = "An error occurred during login", details = ex.Message });
        }
    }

    /// <summary>
    /// Logout endpoint — revokes the current session.
    /// Requires valid session (cookie).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Validate CSRF token
        await _antiforgery.ValidateRequestAsync(HttpContext);

        // Get session ID from cookie
        if (!HttpContext.Request.Cookies.TryGetValue("__Host-evidata.sid", out var sessionId) || 
            string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new { message = "No active session" });
        }

        // Logout
        var command = new LogoutCommand(sessionId);
        var response = await _logoutHandler.HandleAsync(command, ct);

        // Clear session cookie
        Response.Cookies.Delete(
            "__Host-evidata.sid",
            new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/"
            });

        return Ok(response);
    }

    /// <summary>
    /// Get current session status.
    /// Public endpoint (returns 401 if not authenticated instead of redirecting).
    /// </summary>
    [HttpGet("../api/session")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var response = await _getSessionStatusHandler.HandleAsync(ct);
        return Ok(response);
    }

    /// <summary>
    /// Get current authenticated user's profile.
    /// Requires valid session.
    /// </summary>
    [HttpGet("../api/me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        try
        {
            var response = await _getUserProfileHandler.HandleAsync(ct);
            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "User not found" });
        }
    }

    /// <summary>
    /// Get current authenticated user's effective permissions.
    /// Requires valid session.
    /// </summary>
    [HttpGet("../api/permissions")]
    [Authorize]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        try
        {
            var response = await _getUserPermissionsHandler.HandleAsync(ct);
            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "User not found" });
        }
    }

    /// <summary>
    /// Get or refresh CSRF token.
    /// Public endpoint — allows anonymous users to get CSRF token.
    /// </summary>
    [HttpGet("../api/csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponseDto(tokens.RequestToken ?? ""));
    }

    /// <summary>
    /// Maps IdentityErrorCodes to HTTP status codes.
    /// Implements Decision #1: InvitationRevoked maps to 403 in login context.
    /// </summary>
    private static int MapErrorCodeToHttpStatus(string errorCode) =>
        errorCode switch
        {
            IdentityErrorCodes.Unauthenticated => StatusCodes.Status401Unauthorized,
            IdentityErrorCodes.InsufficientPermissions => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.CsrfValidationFailed => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.TenantUnavailable => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.UserNotProvisioned => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.UserSuspended => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.UserDisabled => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.InvitationRevoked => StatusCodes.Status403Forbidden, // Login context
            IdentityErrorCodes.TenantMismatch => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.InvitationEmailMismatch => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.InvitationExpired => StatusCodes.Status403Forbidden,
            IdentityErrorCodes.UserAlreadyExists => StatusCodes.Status409Conflict,
            IdentityErrorCodes.InvalidStateTransition => StatusCodes.Status409Conflict,
            IdentityErrorCodes.LastTenantOwnerBlocked => StatusCodes.Status409Conflict,
            IdentityErrorCodes.InvalidRole => StatusCodes.Status422UnprocessableEntity,
            IdentityErrorCodes.InvalidResponsibleArea => StatusCodes.Status422UnprocessableEntity,
            IdentityErrorCodes.ReasonRequired => StatusCodes.Status422UnprocessableEntity,
            IdentityErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
}
