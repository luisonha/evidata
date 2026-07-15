using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Api;

/// <summary>
/// Development-only authentication endpoints for local/test environments.
/// These endpoints are ONLY available when IHostEnvironment.IsDevelopment() is true.
/// Protected by [DevelopmentOnly] filter that returns 404 in non-dev environments.
/// </summary>
[ApiController]
[Route("dev/auth")]
[AllowAnonymous]  // Dev endpoints don't require auth, but DevelopmentOnly filter ensures dev environment
[DevelopmentOnly] // SECURITY: Blocks all endpoints if not in Development environment (returns 404)
public class DevAuthController : ControllerBase
{
    private readonly IDevAuthService _devAuthService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<DevAuthController> _logger;

    public DevAuthController(
        IDevAuthService devAuthService,
        ICurrentUserContext currentUserContext,
        ILogger<DevAuthController> logger)
    {
        _devAuthService = devAuthService ?? throw new ArgumentNullException(nameof(devAuthService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all available development seed user profiles.
    /// Allows testers/frontend to see which users can be logged in for testing.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// Example response:
    /// ```
    /// {
    ///   "users": [
    ///     { "userId": "00000000-0000-0000-0000-000000000020", "email": "admin@local.evidata", "displayName": "Admin LocalDev", "status": "Active" },
    ///     { "userId": "00000000-0000-0000-0000-000000000021", "email": "compliance@local.evidata", "displayName": "Compliance LocalDev", "status": "Active" }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        try
        {
            var tenantId = _currentUserContext.TenantId;
            if (tenantId == Guid.Empty)
            {
                return BadRequest(new { message = "Tenant context required" });
            }

            var users = await _devAuthService.GetSeedUsersAsync(tenantId, ct);
            return Ok(new DevUsersListResponseDto(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seed users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Performs local authentication without Entra ID.
    /// Accepts an email of a seed user, creates a real session, returns session cookie.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// This login creates a REAL session (same as production).
    /// 
    /// Request body:
    /// ```
    /// {
    ///   "email": "admin@local.evidata"
    /// }
    /// ```
    /// 
    /// Response:
    /// ```
    /// {
    ///   "userId": "00000000-0000-0000-0000-000000000020",
    ///   "tenantId": "00000000-0000-0000-0000-000000000001",
    ///   "email": "admin@local.evidata",
    ///   "displayName": "Admin LocalDev",
    ///   "roleIds": ["00000000-0000-0000-0000-000000000001"],
    ///   "sessionId": "base64-opaque-session-id",
    ///   "sessionExpiresAt": "2026-07-15T02:00:00Z"
    /// }
    /// ```
    /// 
    /// Sets `__Host-evidata.sid` secure cookie.
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] DevLoginRequestDto request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var tenantId = _currentUserContext.TenantId;
            if (tenantId == Guid.Empty)
            {
                return BadRequest(new { message = "Tenant context required" });
            }

            var (response, errorCode) = await _devAuthService.LoginWithEmailAsync(
                request.Email,
                tenantId,
                ct);

            if (errorCode is not null)
            {
                var statusCode = MapErrorCodeToHttpStatus(errorCode);
                return StatusCode(statusCode, new { errorCode, message = errorCode });
            }

            if (response is null)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Login failed" });
            }

            // Set secure session cookie (same as production callback)
            Response.Cookies.Append(
                "__Host-evidata.sid",
                response.SessionId,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = response.SessionExpiresAt
                });

            _logger.LogInformation("Dev login successful: {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dev login");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Expires/revokes the current session.
    /// Useful for testing session expiration handling.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// Requires active session (will be read from __Host-evidata.sid cookie).
    /// </remarks>
    [HttpPost("expire-session")]
    public async Task<IActionResult> ExpireSession(CancellationToken ct)
    {
        try
        {
            if (!HttpContext.Request.Cookies.TryGetValue("__Host-evidata.sid", out var sessionId) ||
                string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "No active session" });
            }

            var success = await _devAuthService.ExpireCurrentSessionAsync(sessionId, ct);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to expire session" });
            }

            // Clear cookie
            Response.Cookies.Delete(
                "__Host-evidata.sid",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            _logger.LogInformation("Dev session expired");
            return Ok(new ExpireSessionResponseDto(true, "Session expired successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring session");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Simulates specific authorization/authentication scenarios.
    /// Mutates user state to test authorization pipeline behavior.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// 
    /// Scenarios:
    /// - "denied": Removes all roles from user (tests authorization failure)
    /// - "disabled": Sets user status to Disabled (tests disabled user rejection)
    /// - "no-tenant": Moves user to different tenant (tests tenant isolation)
    /// 
    /// These mutations are REAL — the authorization pipeline will reject them normally.
    /// 
    /// Request body:
    /// ```
    /// {
    ///   "email": "admin@local.evidata",
    ///   "scenario": "denied"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("set-scenario")]
    public async Task<IActionResult> SetScenario(
        [FromBody] SetScenarioRequestDto request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Scenario))
        {
            return BadRequest(new { message = "Email and Scenario are required" });
        }

        try
        {
            var (success, errorCode) = await _devAuthService.SetScenarioAsync(
                request.Email,
                request.Scenario,
                ct);

            if (errorCode is not null)
            {
                var statusCode = MapErrorCodeToHttpStatus(errorCode);
                return StatusCode(statusCode, new { errorCode, message = errorCode });
            }

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to set scenario" });
            }

            _logger.LogInformation("Dev scenario set: {Email} -> {Scenario}", request.Email, request.Scenario);
            return Ok(new SetScenarioResponseDto(true, "Scenario applied successfully", request.Scenario));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting scenario");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Logs out the current session (alias for expire-session with cleaner semantics).
    /// Invalidates the session and clears the cookie.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// Similar to production POST /auth/logout but without CSRF validation (dev-only).
    /// </remarks>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            if (!HttpContext.Request.Cookies.TryGetValue("__Host-evidata.sid", out var sessionId) ||
                string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "No active session" });
            }

            var success = await _devAuthService.ExpireCurrentSessionAsync(sessionId, ct);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to logout" });
            }

            // Clear cookie
            Response.Cookies.Delete(
                "__Host-evidata.sid",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            _logger.LogInformation("Dev logout successful");
            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dev logout");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Maps error codes to HTTP status codes.
    /// </summary>
    private static int MapErrorCodeToHttpStatus(string errorCode) =>
        errorCode switch
        {
            "UserNotFound" => StatusCodes.Status404NotFound,
            "UserDisabled" => StatusCodes.Status403Forbidden,
            "UserSuspended" => StatusCodes.Status403Forbidden,
            "InternalError" => StatusCodes.Status500InternalServerError,
            "InvalidInput" => StatusCodes.Status422UnprocessableEntity,
            "UserAlreadyExists" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}
