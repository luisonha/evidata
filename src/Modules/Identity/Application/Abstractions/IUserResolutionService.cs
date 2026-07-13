using Evidata.Modules.Identity.Application.DTOs;

namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Service for resolving Evidata users from Entra ID login attempts.
/// Implements the resolution order from doc02:
/// 1. tenantId + entraOid
/// 2. tenantId + normalizedEmail
/// 3. tenantId + pending invitation
/// </summary>
public interface IUserResolutionService
{
    /// <summary>
    /// Resolves a user from Entra ID claims during login callback.
    /// Follows the documented resolution order and returns appropriate error codes.
    /// </summary>
    /// <param name="claims">Validated claims from Entra ID token</param>
    /// <param name="tenantId">The tenant context for this request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// A tuple of (user, invitation, errorCode).
    /// If resolution succeeds: (userProfile, null, null)
    /// If resolution finds an invitation: (null, invitation, null)
    /// If resolution fails: (null, null, errorCode)
    /// Error codes follow IdentityErrorCodes catalog.
    /// </returns>
    Task<(Domain.UserProfile? User, Domain.Invitation? Invitation, string? ErrorCode)> ResolveUserAsync(
        EntraIdTokenClaimsDto claims,
        Guid tenantId,
        CancellationToken ct = default);
}
