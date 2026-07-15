using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Service for development/local authentication endpoints.
/// These operations are ONLY available in Development environment.
/// </summary>
public interface IDevAuthService
{
    /// <summary>
    /// Authenticates a user by email from the seed database.
    /// Creates a real session via ISessionService (same as production login).
    /// </summary>
    Task<(DevLoginResponseDto? Response, string? ErrorCode)> LoginWithEmailAsync(
        string email,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all available development seed user profiles.
    /// </summary>
    Task<IReadOnlyList<DevUserProfileDto>> GetSeedUsersAsync(
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Expires/revokes the current session for the authenticated user.
    /// </summary>
    Task<bool> ExpireCurrentSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Simulates a scenario (denied access, disabled user, no tenant) by mutating user state.
    /// The mutations are REAL — the rest of the authorization pipeline will treat them normally.
    /// </summary>
    Task<(bool Success, string? ErrorCode)> SetScenarioAsync(
        string email,
        string scenario,  // "denied", "disabled", "no-tenant"
        CancellationToken ct = default);

    /// <summary>
    /// Resets all seed users to their original state.
    /// </summary>
    Task<int> ResetSeedUsersAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new ad-hoc scenario user for admin testing (e.g., pending invitation).
    /// </summary>
    Task<(Guid UserId, string? ErrorCode)> CreateScenarioUserAsync(
        string scenarioType,
        string email,
        string? displayName,
        Guid tenantId,
        CancellationToken ct = default);
}
