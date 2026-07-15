using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Services;

/// <summary>
/// Handles admin user management operations (invitations, status changes, role changes).
/// Audit events are emitted by the controller after successful operations.
/// </summary>
public class AdminUsersService
{
    private readonly IUserProfileRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly ISessionService _sessionService;
    private readonly IRoleNameResolver _roleNameResolver;

    public AdminUsersService(
        IUserProfileRepository userRepository,
        IInvitationRepository invitationRepository,
        ISessionService sessionService,
        IRoleNameResolver roleNameResolver)
    {
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _sessionService = sessionService;
        _roleNameResolver = roleNameResolver;
    }

    public async Task<ListUsersResponseDto> ListUsersAsync(ListUsersQuery query, CancellationToken ct)
    {
        var users = await _userRepository.GetByTenantIdAsync(query.TenantId, ct);
        var items = users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var q = query.SearchQuery.ToLowerInvariant();
            items = items.Where(u => u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) || u.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (query.StatusFilter.HasValue)
            items = items.Where(u => u.Status == query.StatusFilter.Value);

        if (!string.IsNullOrWhiteSpace(query.RoleFilter))
        {
            var roleId = await _roleNameResolver.GetRoleIdByNameAsync(query.RoleFilter, ct);
            if (roleId.HasValue)
                items = items.Where(u => u.HasRole(roleId.Value));
            else
                items = items.Where(u => false); // Filter doesn't match any role
        }

        var total = items.Count();
        var pageItems = new List<AdminUserListItemDto>();
        foreach (var user in items.OrderByDescending(u => u.UpdatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize))
        {
            var roleNames = await _roleNameResolver.GetRoleNamesAsync(user.GetRoleIds(), ct);
            pageItems.Add(new AdminUserListItemDto(user.Id, user.Email, user.DisplayName, user.Status, roleNames, null, user.LastLoginAt));
        }

        return new(pageItems, query.Page, query.PageSize, total);
    }

    public async Task<AdminUserDetailDto> GetUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");
        
        var roleNames = await _roleNameResolver.GetRoleNamesAsync(user.GetRoleIds(), ct);
        return new(user.Id, user.Email, user.DisplayName, user.Status, roleNames, null, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);
    }

    public async Task<InviteUserResponseDto> InviteUserAsync(InviteUserCommand cmd, CancellationToken ct)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        var existing = await _userRepository.GetByEmailAsync(email, cmd.TenantId, ct);
        if (existing is not null && existing.Status != UserStatus.InvitationRevoked)
            throw new IdentityDomainException(IdentityErrorCodes.UserAlreadyExists, "Email already in use");

        var existingInv = await _invitationRepository.GetByEmailAndTenantAsync(email, cmd.TenantId, ct);
        if (existingInv?.Status == InvitationStatus.Pending)
            throw new IdentityDomainException(IdentityErrorCodes.UserAlreadyExists, "Use resend-invitation instead");

        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", email, cmd.DisplayName, cmd.TenantId);
        await _userRepository.UpsertAsync(user, ct);

        var inv = Invitation.Create(cmd.TenantId, email, cmd.Role, cmd.CreatedByUserId, DateTime.UtcNow.AddDays(7), cmd.ResponsibleAreaId, cmd.Message);
        await _invitationRepository.UpsertAsync(inv, ct);

        var userDto = new AdminUserDetailDto(user.Id, user.Email, user.DisplayName, user.Status, new[] { cmd.Role }, cmd.ResponsibleAreaId, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);
        var invDto = new InvitationDetailDto(inv.Id, inv.Status.ToString(), inv.ExpiresAt, inv.CreatedAt);
        return new(userDto, invDto);
    }

    public async Task<ResendInvitationResponseDto> ResendInvitationAsync(ResendInvitationCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        // Invitation.UserId stays null until the invitee logs in and calls Accept() — so a still-pending
        // invitation can only be looked up by email/tenant, not by the admin-created UserProfile.Id.
        var inv = await _invitationRepository.GetByEmailAndTenantAsync(user.Email, cmd.TenantId, ct);
        if (inv is null || inv.Status != InvitationStatus.Pending)
            throw new IdentityDomainException(IdentityErrorCodes.InvitationRevoked, "No pending invitation");

        var newExpiresAt = DateTime.UtcNow.AddDays(7);
        inv.ExtendExpiry(newExpiresAt);
        await _invitationRepository.UpsertAsync(inv, ct);
        return new(cmd.UserId, inv.Id, newExpiresAt, DateTime.UtcNow);
    }

    public async Task<RevokeInvitationResponseDto> RevokeInvitationAsync(RevokeInvitationCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        if (user.Status != UserStatus.Invited)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Invalid user state");

        // Same rationale as ResendInvitationAsync: look up by email/tenant, not by UserId.
        var inv = await _invitationRepository.GetByEmailAndTenantAsync(user.Email, cmd.TenantId, ct);
        if (inv is null) throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "Invitation not found");

        inv.Revoke();
        user.RevokeInvitation();
        await _invitationRepository.UpsertAsync(inv, ct);
        await _userRepository.UpsertAsync(user, ct);
        return new(cmd.UserId, inv.Id, inv.Status.ToString(), inv.UpdatedAt);
    }

    public async Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        if (!string.IsNullOrWhiteSpace(cmd.DisplayName) && user.DisplayName != cmd.DisplayName)
            user.UpdateProfile(user.Email, cmd.DisplayName);

        await _userRepository.UpsertAsync(user, ct);

        var roleNames = await _roleNameResolver.GetRoleNamesAsync(user.GetRoleIds(), ct);
        var userDto = new AdminUserDetailDto(user.Id, user.Email, user.DisplayName, user.Status, roleNames, cmd.ResponsibleAreaId, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);
        return new(userDto, user.UpdatedAt);
    }

    public async Task<SuspendUserResponseDto> SuspendUserAsync(SuspendUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");
        
        if (user.Status != UserStatus.Active)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot suspend this user");

        // Check if user has TenantOwner role. Fail closed (block the mutation) if the role
        // catalog cannot resolve "TenantOwner" at all — this indicates a data-integrity problem
        // and must never be treated as "user is not a TenantOwner" (that would silently disable
        // the guard).
        var tenantOwnerRoleId = await _roleNameResolver.GetRoleIdByNameAsync("TenantOwner", ct)
            ?? throw new InvalidOperationException("TenantOwner role could not be resolved from the role catalog");
        if (user.HasRole(tenantOwnerRoleId))
        {
            var count = await _userRepository.CountActiveTenantOwnersAsync(cmd.TenantId, ct);
            if (count <= 1) throw new IdentityDomainException(IdentityErrorCodes.LastTenantOwnerBlocked, "Cannot suspend last TenantOwner");
        }

        user.Suspend();
        await _userRepository.UpsertAsync(user, ct);
        await _sessionService.RevokeAllSessionsForUserAsync(cmd.UserId, cmd.TenantId, ct);
        return new(user.Id, user.Status, user.UpdatedAt);
    }

    public async Task<ReactivateUserResponseDto> ReactivateUserAsync(ReactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        if (user.Status != UserStatus.Suspended && user.Status != UserStatus.Disabled)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot reactivate this user");

        user.Reactivate();
        await _userRepository.UpsertAsync(user, ct);
        return new(user.Id, user.Status, user.UpdatedAt);
    }

    public async Task<DisableUserResponseDto> DisableUserAsync(DisableUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        if (user.Status != UserStatus.Active && user.Status != UserStatus.Suspended)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot disable this user");

        // Check if user has TenantOwner role. Fail closed if the role catalog cannot resolve
        // "TenantOwner" — never silently disable the guard.
        var tenantOwnerRoleId = await _roleNameResolver.GetRoleIdByNameAsync("TenantOwner", ct)
            ?? throw new InvalidOperationException("TenantOwner role could not be resolved from the role catalog");
        if (user.HasRole(tenantOwnerRoleId))
        {
            var count = await _userRepository.CountActiveTenantOwnersAsync(cmd.TenantId, ct);
            if (count <= 1) throw new IdentityDomainException(IdentityErrorCodes.LastTenantOwnerBlocked, "Cannot disable last TenantOwner");
        }

        user.Disable();
        await _userRepository.UpsertAsync(user, ct);
        await _sessionService.RevokeAllSessionsForUserAsync(cmd.UserId, cmd.TenantId, ct);
        return new(user.Id, user.Status, user.UpdatedAt);
    }

    public async Task<ChangeUserRolesResponseDto> ChangeUserRolesAsync(ChangeUserRolesCommand cmd, CancellationToken ct)
    {
        if (!cmd.Roles.Any()) throw new IdentityDomainException(IdentityErrorCodes.InvalidRole, "At least one role required");

        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");

        // Get current role names for validation
        var currentRoleNames = await _roleNameResolver.GetRoleNamesAsync(user.GetRoleIds(), ct);
        
        // Check if trying to remove TenantOwner role while it's the last one
        if (currentRoleNames.Contains("TenantOwner") && !cmd.Roles.Contains("TenantOwner") && user.Status == UserStatus.Active)
        {
            var count = await _userRepository.CountActiveTenantOwnersAsync(cmd.TenantId, ct);
            if (count <= 1) throw new IdentityDomainException(IdentityErrorCodes.LastTenantOwnerBlocked, "Cannot remove last TenantOwner");
        }

        // Resolve role names to IDs and update user's roles
        var newRoleIds = await _roleNameResolver.GetRoleIdsByNamesAsync(cmd.Roles, ct);
        user.SetRoles(newRoleIds);
        
        await _userRepository.UpsertAsync(user, ct);
        return new(user.Id, cmd.Roles, user.UpdatedAt);
    }
}
