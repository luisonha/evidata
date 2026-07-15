using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Api;

/// <summary>
/// Development-only administrative endpoints for local/test environments.
/// These endpoints manage test data and scenarios for Phase 3+ testing.
/// ONLY available when IHostEnvironment.IsDevelopment() is true.
/// Protected by [DevelopmentOnly] filter that returns 404 in non-dev environments.
/// </summary>
[ApiController]
[Route("dev/admin")]
[AllowAnonymous]  // Dev endpoints don't require auth, but DevelopmentOnly filter ensures dev environment
[DevelopmentOnly] // SECURITY: Blocks all endpoints if not in Development environment (returns 404)
public class DevAdminController : ControllerBase
{
    private readonly IDevAuthService _devAuthService;
    private readonly ILogger<DevAdminController> _logger;

    public DevAdminController(
        IDevAuthService devAuthService,
        ILogger<DevAdminController> logger)
    {
        _devAuthService = devAuthService ?? throw new ArgumentNullException(nameof(devAuthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resets all development seed users to their original state.
    /// Reverts any mutations made by set-scenario or other test operations.
    /// Useful for ensuring clean test state before each test suite.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// 
    /// This endpoint:
    /// - Restores user status (e.g., Disabled -> Active, Suspended -> Active)
    /// - Restores role assignments
    /// - Clears login timestamps
    /// - Restores tenant assignments
    /// 
    /// Returns the count of users reset.
    /// </remarks>
    [HttpPost("users/reset-seed")]
    public async Task<IActionResult> ResetSeedUsers(CancellationToken ct)
    {
        try
        {
            var count = await _devAuthService.ResetSeedUsersAsync(ct);
            _logger.LogInformation("Reset {Count} seed users to original state", count);
            return Ok(new ResetSeedResponseDto(true, "Seed users reset successfully", count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting seed users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates an ad-hoc scenario user for administrative testing.
    /// Allows Phase 4+ tests to create users with specific states without modifying seed data.
    /// </summary>
    /// <remarks>
    /// DEVELOPMENT ONLY.
    /// 
    /// ScenarioTypes:
    /// - "pending-invitation": Creates a user with Invited status (for invitation workflow testing)
    /// - "suspended": Creates a suspended user (for reactivation testing)
    /// - "disabled": Creates a disabled user (for re-enabling testing)
    /// 
    /// Request body:
    /// ```
    /// {
    ///   "scenarioType": "pending-invitation",
    ///   "email": "newuser@test.local",
    ///   "displayName": "Test User",
    ///   "role": null
    /// }
    /// ```
    /// 
    /// Response:
    /// ```
    /// {
    ///   "success": true,
    ///   "message": "Scenario user created successfully",
    ///   "userId": "12345678-1234-5678-1234-567812345678",
    ///   "email": "newuser@test.local"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("users/create-scenario")]
    public async Task<IActionResult> CreateScenarioUser(
        [FromBody] CreateScenarioRequestDto request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.ScenarioType))
        {
            return BadRequest(new { message = "Email and ScenarioType are required" });
        }

        try
        {
            var tenantId = new Guid("00000000-0000-0000-0000-000000000001"); // Default tenant

            var (userId, errorCode) = await _devAuthService.CreateScenarioUserAsync(
                request.ScenarioType,
                request.Email,
                request.DisplayName,
                tenantId,
                ct);

            if (errorCode is not null)
            {
                var statusCode = MapErrorCodeToHttpStatus(errorCode);
                return StatusCode(statusCode, new { errorCode, message = errorCode });
            }

            if (userId == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to create scenario user" });
            }

            _logger.LogInformation(
                "Created scenario user {UserId} ({Email}) with type {ScenarioType}",
                userId, request.Email, request.ScenarioType);

            return Ok(new CreateScenarioResponseDto(true, "Scenario user created successfully", userId, request.Email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario user");
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
            "UserAlreadyExists" => StatusCodes.Status409Conflict,
            "InternalError" => StatusCodes.Status500InternalServerError,
            "InvalidInput" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
}
