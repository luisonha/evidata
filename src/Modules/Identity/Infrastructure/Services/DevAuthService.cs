using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Development authentication service.
/// Manages local dev login, session expiration, and scenario simulation.
/// ONLY available in Development environment (enforced via middleware).
/// </summary>
public class DevAuthService : IDevAuthService
{
    private readonly IdentityDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<DevAuthService> _logger;

    // Mapping of scenario names to actions
    private readonly Dictionary<string, Func<UserProfile, Task>> _scenarioHandlers;

    public DevAuthService(
        IdentityDbContext context,
        ISessionService sessionService,
        ILogger<DevAuthService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _scenarioHandlers = new Dictionary<string, Func<UserProfile, Task>>
        {
            { "denied", SetScenarioDenied },      // Remove all roles
            { "disabled", SetScenarioDisabled },  // Change status to Disabled
            { "no-tenant", SetScenarioNoTenant }  // Move to Tenant2
        };
    }

    public async Task<(DevLoginResponseDto? Response, string? ErrorCode)> LoginWithEmailAsync(
        string email,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (null, IdentityErrorCodes.UserNotFound);

        try
        {
            // Find user by email in the seed users
            var user = await _context.UserProfiles
                .Include(u => u.UserProfileRoles)
                .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId, ct);

            if (user is null)
            {
                _logger.LogWarning("Dev login attempt with non-existent user email: {Email}", email);
                return (null, IdentityErrorCodes.UserNotFound);
            }

            // Check user status
            if (user.Status == UserStatus.Disabled)
            {
                _logger.LogWarning("Dev login attempt with disabled user: {Email}", email);
                return (null, IdentityErrorCodes.UserDisabled);
            }

            if (user.Status == UserStatus.Suspended)
            {
                _logger.LogWarning("Dev login attempt with suspended user: {Email}", email);
                return (null, IdentityErrorCodes.UserSuspended);
            }

            // Create session using the real ISessionService
            var roleIds = user.GetRoleIds().ToList();
            var sessionId = await _sessionService.CreateSessionAsync(
                user.Id,
                tenantId,
                roleIds,
                ct: ct);

            // Record login
            user.RecordLogin();
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Dev login successful for user {UserId} ({Email})", user.Id, email);

            var response = new DevLoginResponseDto(
                user.Id,
                tenantId,
                user.Email,
                user.DisplayName,
                roleIds,
                sessionId,
                DateTime.UtcNow.AddHours(1)  // 1 hour expiry (matches production)
            );

            return (response, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dev login for email {Email}", email);
            return (null, IdentityErrorCodes.Unauthenticated);
        }
    }

    public async Task<IReadOnlyList<DevUserProfileDto>> GetSeedUsersAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        try
        {
            var users = await _context.UserProfiles
                .Where(u => u.TenantId == tenantId || u.TenantId == LocalDevSeedUsers.DefaultTenantId)
                .OrderBy(u => u.Email)
                .ToListAsync(ct);

            var result = users.Select(u => new DevUserProfileDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.Status.ToString()
            )).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seed users");
            return [];
        }
    }

    public async Task<bool> ExpireCurrentSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        try
        {
            var result = await _sessionService.RevokeSessionAsync(sessionId, ct);
            if (result)
            {
                _logger.LogInformation("Dev session expired: {SessionId}", sessionId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring dev session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<(bool Success, string? ErrorCode)> SetScenarioAsync(
        string email,
        string scenario,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(scenario))
            return (false, IdentityErrorCodes.InvalidRole);

        if (!_scenarioHandlers.TryGetValue(scenario.ToLower(), out var handler))
        {
            _logger.LogWarning("Unknown scenario: {Scenario}", scenario);
            return (false, IdentityErrorCodes.InvalidRole);
        }

        try
        {
            var user = await _context.UserProfiles
                .Include(u => u.UserProfileRoles)
                .FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null)
                return (false, IdentityErrorCodes.UserNotFound);

            await handler(user);

            // Special handling for "no-tenant" scenario: update TenantId via raw SQL
            if (scenario.ToLower() == "no-tenant")
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE identity.user_profiles SET tenant_id = '00000000-0000-0000-0000-000000000002' WHERE id = {user.Id}",
                    ct);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Dev scenario '{Scenario}' applied to user {UserId} ({Email})",
                scenario, user.Id, email);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting scenario '{Scenario}' for {Email}", scenario, email);
            return (false, IdentityErrorCodes.InvalidRole);
        }
    }

    public async Task<int> ResetSeedUsersAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Reset all users back to their seed state
            // This is simplistic — in a real system, you'd load from a seed definition
            var seedEmails = new[]
            {
                "admin@local.evidata",
                "compliance@local.evidata",
                "owner@local.evidata",
                "reviewer@local.evidata",
                "viewer@local.evidata",
                "invited@local.evidata",
                "suspended@local.evidata",
                "disabled@local.evidata",
                "denied@local.evidata",
                "no-tenant@local.evidata"
            };

            var users = await _context.UserProfiles
                .Include(u => u.UserProfileRoles)
                .Where(u => seedEmails.Contains(u.Email))
                .ToListAsync(ct);

            foreach (var user in users)
            {
                // Reset to original seed state based on email
                ResetUserToSeedState(user);
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Reset {Count} seed users to original state", users.Count);

            return users.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting seed users");
            return 0;
        }
    }

    public async Task<(Guid UserId, string? ErrorCode)> CreateScenarioUserAsync(
        string scenarioType,
        string email,
        string? displayName,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(scenarioType))
            return (Guid.Empty, IdentityErrorCodes.InvalidRole);

        try
        {
            // Check if user already exists
            var existing = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Email == email, ct);

            if (existing is not null)
                return (Guid.Empty, IdentityErrorCodes.UserAlreadyExists);

            // Create user based on scenario type
            var user = UserProfile.Create(
                email,
                "local",
                email,
                displayName ?? email.Split('@')[0],
                tenantId);

            // Apply scenario-specific state
            switch (scenarioType.ToLower())
            {
                case "pending-invitation":
                    // Already created with Invited status
                    break;
                case "suspended":
                    user.Activate();  // Must activate first before suspending
                    user.Suspend();
                    break;
                case "disabled":
                    user.Activate();  // Must activate first before disabling
                    user.Disable();
                    break;
                default:
                    return (Guid.Empty, IdentityErrorCodes.InvalidRole);
            }

            _context.UserProfiles.Add(user);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created scenario user {UserId} ({Email}) with type {ScenarioType}",
                user.Id, email, scenarioType);

            return (user.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario user {Email}", email);
            return (Guid.Empty, IdentityErrorCodes.InvalidRole);
        }
    }

    // ── Scenario Handlers ──────────────────────────────────────────────

    private Task SetScenarioDenied(UserProfile user)
    {
        // Remove all roles
        user.SetRoles([]);
        return Task.CompletedTask;
    }

    private Task SetScenarioDisabled(UserProfile user)
    {
        // Change status to Disabled
        if (user.Status == UserStatus.Active)
            user.Disable();
        else if (user.Status == UserStatus.Suspended)
        {
            user.Reactivate();  // Go to Active first
            user.Disable();      // Then disable
        }
        // If already Disabled, Invited, or InvitationRevoked, do nothing

        return Task.CompletedTask;
    }

    private Task SetScenarioNoTenant(UserProfile user)
    {
        // For "no-tenant" scenario, we simulate a user whose tenant is unavailable
        // by updating the TenantId directly via EF Core change tracking
        // This is a dev-only operation that wouldn't happen in production
        return Task.CompletedTask;  // Will be handled via SQL in the service method
    }

    private void ResetUserToSeedState(UserProfile user)
    {
        // Reset each user to their original seed state
        user.SetRoles([]);
        
        var email = user.Email;

        // Restore state based on seed data
        var targetStatus = email switch
        {
            "admin@local.evidata" => UserStatus.Active,
            "compliance@local.evidata" => UserStatus.Active,
            "owner@local.evidata" => UserStatus.Active,
            "reviewer@local.evidata" => UserStatus.Active,
            "viewer@local.evidata" => UserStatus.Active,
            "invited@local.evidata" => UserStatus.Invited,
            "suspended@local.evidata" => UserStatus.Suspended,
            "disabled@local.evidata" => UserStatus.Disabled,
            "denied@local.evidata" => UserStatus.Active,
            "no-tenant@local.evidata" => UserStatus.Active,
            _ => UserStatus.Active
        };

        // Transition to target status using domain methods
        if (user.Status != targetStatus)
        {
            switch (targetStatus)
            {
                case UserStatus.Active:
                    if (user.Status == UserStatus.Invited)
                        user.Activate();
                    else if (user.Status is UserStatus.Suspended or UserStatus.Disabled)
                        user.Reactivate();
                    break;
                case UserStatus.Suspended:
                    if (user.Status == UserStatus.Active)
                        user.Suspend();
                    break;
                case UserStatus.Disabled:
                    if (user.Status is UserStatus.Active or UserStatus.Suspended)
                        user.Disable();
                    break;
                case UserStatus.Invited:
                    // Can't easily reset to Invited from other states with domain methods
                    // This is a dev-only scenario, so it's acceptable to skip
                    break;
            }
        }

        // Restore roles based on seed data
        var roleIds = email switch
        {
            "admin@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000001") }, // TenantOwner
            "compliance@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000002") }, // ComplianceAdmin
            "owner@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000003") }, // ProcessOwner
            "reviewer@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000004") }, // LegalReviewer
            "viewer@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000007") }, // Viewer
            "invited@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000003") }, // ProcessOwner
            "suspended@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000007") }, // Viewer
            "disabled@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000007") }, // Viewer
            "denied@local.evidata" => Array.Empty<Guid>(),
            "no-tenant@local.evidata" => new[] { new Guid("00000000-0000-0000-0000-000000000001") }, // TenantOwner
            _ => Array.Empty<Guid>()
        };

        user.SetRoles(roleIds);
    }
}
