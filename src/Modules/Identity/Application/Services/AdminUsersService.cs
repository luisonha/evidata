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

    public AdminUsersService(
        IUserProfileRepository userRepository,
        IInvitationRepository invitationRepository,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _sessionService = sessionService;
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
            items = items.Where(u => u.HasRoleName(query.RoleFilter));

        var total = items.Count();
        var pageItems = items.OrderByDescending(u => u.UpdatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(u => new AdminUserListItemDto(u.Id, u.Email, u.DisplayName, u.Status, u.GetRoleNames(), null, u.LastLoginAt)).ToList();

        return new(pageItems, query.Page, query.PageSize, total);
    }

    public async Task<AdminUserDetailDto> GetUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId)
            throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "User not found");
        return new(user.Id, user.Email, user.DisplayName, user.Status, user.GetRoleNames(), null, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);
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
        var inv = await _invitationRepository.GetByUserIdAsync(cmd.UserId, ct);
        if (inv is null || inv.TenantId != cmd.TenantId || inv.Status != InvitationStatus.Pending)
            throw new IdentityDomainException(IdentityErrorCodes.InvitationRevoked, "No pending invitation");

        var newExpiresAt = DateTime.UtcNow.AddDays(7);
        // For now, just update the expiry time conceptually
        // Note: Invitation is immutable, so a full update would require deleting and recreating
        return new(cmd.UserId, inv.Id, newExpiresAt, DateTime.UtcNow);
    }

    public async Task<RevokeInvitationResponseDto> RevokeInvitationAsync(RevokeInvitationCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId || user.Status != UserStatus.Invited)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Invalid user state");

        var inv = await _invitationRepository.GetByUserIdAsync(cmd.UserId, ct);
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

        var userDto = new AdminUserDetailDto(user.Id, user.Email, user.DisplayName, user.Status, user.GetRoleNames(), cmd.ResponsibleAreaId, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);
        return new(userDto, user.UpdatedAt);
    }

    public async Task<SuspendUserResponseDto> SuspendUserAsync(SuspendUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId || user.Status != UserStatus.Active)
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot suspend this user");

        if (user.HasRoleName("TenantOwner"))
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
        if (user is null || user.TenantId != cmd.TenantId || (user.Status != UserStatus.Suspended && user.Status != UserStatus.Disabled))
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot reactivate this user");

        user.Reactivate();
        await _userRepository.UpsertAsync(user, ct);
        return new(user.Id, user.Status, user.UpdatedAt);
    }

    public async Task<DisableUserResponseDto> DisableUserAsync(DisableUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.TenantId != cmd.TenantId || (user.Status != UserStatus.Active && user.Status != UserStatus.Suspended))
            throw new IdentityDomainException(IdentityErrorCodes.InvalidStateTransition, "Cannot disable this user");

        if (user.HasRoleName("TenantOwner"))
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

        var currentRoles = user.GetRoleNames();
        if (currentRoles.Contains("TenantOwner") && !cmd.Roles.Contains("TenantOwner") && user.Status == UserStatus.Active)
        {
            var count = await _userRepository.CountActiveTenantOwnersAsync(cmd.TenantId, ct);
            if (count <= 1) throw new IdentityDomainException(IdentityErrorCodes.LastTenantOwnerBlocked, "Cannot remove last TenantOwner");
        }

        await _userRepository.UpsertAsync(user, ct);
        return new(user.Id, cmd.Roles, user.UpdatedAt);
    }
}
