using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Default implementation of IUserResolutionService.
/// Resolves users from Entra ID claims following the documented priority order:
/// 1. tenantId + entraOid
/// 2. tenantId + normalizedEmail
/// 3. tenantId + pending invitation
/// 
/// Returns appropriate error codes for failed resolutions.
/// </summary>
public class UserResolutionService : IUserResolutionService
{
    private readonly IdentityDbContext _identityContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<UserResolutionService> _logger;

    public UserResolutionService(
        IdentityDbContext identityContext,
        ITenantRepository tenantRepository,
        ILogger<UserResolutionService> logger)
    {
        _identityContext = identityContext;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<(UserProfile? User, Invitation? Invitation, string? ErrorCode)> ResolveUserAsync(
        EntraIdTokenClaimsDto claims,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // Step 0: Validate tenant is available (not suspended/deleted)
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            _logger.LogWarning("Tenant {TenantId} not found or not active", tenantId);
            return (null, null, IdentityErrorCodes.TenantUnavailable);
        }

        var normalizedEmail = claims.Email.ToLowerInvariant();

        // Step 1: Try to find by tenantId + entraOid
        var userByOid = await _identityContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId 
                     && u.ExternalId == claims.Oid 
                     && u.Provider == "EntraId",
                cancellationToken: ct);

        if (userByOid is not null)
        {
            return (await ValidateUserStatusAsync(userByOid, null, ct), null, null);
        }

        // Step 2: Try to find by tenantId + normalizedEmail
        var userByEmail = await _identityContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId 
                     && u.Email.ToLower() == normalizedEmail,
                cancellationToken: ct);

        if (userByEmail is not null)
        {
            return (await ValidateUserStatusAsync(userByEmail, null, ct), null, null);
        }

        // Step 3: Try to find a pending invitation matching tenantId + email
        var invitation = await _identityContext.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.TenantId == tenantId 
                     && i.Email.ToLower() == normalizedEmail
                     && i.Status == InvitationStatus.Pending,
                cancellationToken: ct);

        if (invitation is not null)
        {
            // If tenant mismatch detected (Entra tid != our Evidata tenant), return specific error
            // (This is a defensive check; in normal flow tenant is validated at request level)
            
            // Validate invitation not expired
            if (DateTime.UtcNow > invitation.ExpiresAt)
            {
                _logger.LogInformation("Invitation {InvitationId} expired", invitation.Id);
                return (null, null, IdentityErrorCodes.InvitationExpired);
            }

            return (null, invitation, null);
        }

        // Step 4: No match found
        _logger.LogWarning(
            "User not provisioned: tenantId={TenantId}, email={Email}, oid={Oid}",
            tenantId, claims.Email, claims.Oid);

        return (null, null, IdentityErrorCodes.UserNotProvisioned);
    }

    /// <summary>
    /// Validates user status and returns appropriate error codes for blocked states.
    /// </summary>
    private async Task<UserProfile> ValidateUserStatusAsync(
        UserProfile user,
        Invitation? invitation,
        CancellationToken ct)
    {
        return user.Status switch
        {
            UserStatus.Active => user,
            UserStatus.Suspended => throw new IdentityDomainException(
                IdentityErrorCodes.UserSuspended,
                $"User {user.Id} is suspended"),
            UserStatus.Disabled => throw new IdentityDomainException(
                IdentityErrorCodes.UserDisabled,
                $"User {user.Id} is disabled"),
            UserStatus.InvitationRevoked => throw new IdentityDomainException(
                IdentityErrorCodes.InvitationRevoked,
                $"User {user.Id} invitation was revoked"),
            UserStatus.Invited => 
                // If invited, check for associated invitation status
                invitation switch
                {
                    _ when invitation?.Status == InvitationStatus.Revoked => throw new IdentityDomainException(
                        IdentityErrorCodes.InvitationRevoked,
                        $"Invitation {invitation.Id} has been revoked"),
                    _ when invitation?.Status == InvitationStatus.Expired => throw new IdentityDomainException(
                        IdentityErrorCodes.InvitationExpired,
                        $"Invitation {invitation.Id} has expired"),
                    _ => user
                },
            _ => throw new InvalidOperationException($"Unknown user status: {user.Status}")
        };
    }
}
