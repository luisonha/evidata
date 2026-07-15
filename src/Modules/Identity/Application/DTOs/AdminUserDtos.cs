using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.DTOs;

public record InviteUserRequestDto(string Email, string DisplayName, string Role, Guid? ResponsibleAreaId = null, string? Message = null);
public record AdminUserDetailDto(Guid Id, string Email, string DisplayName, UserStatus Status, IReadOnlyList<string> Roles, Guid? ResponsibleAreaId, DateTime? LastLoginAt, DateTime CreatedAt, DateTime UpdatedAt);
public record InvitationDetailDto(Guid Id, string Status, DateTime ExpiresAt, DateTime CreatedAt);
public record InviteUserResponseDto(AdminUserDetailDto User, InvitationDetailDto Invitation);
public record ResendInvitationRequestDto(string? Reason = null);
public record ResendInvitationResponseDto(Guid UserId, Guid InvitationId, DateTime NewExpiresAt, DateTime UpdatedAt);
public record RevokeInvitationRequestDto(string Reason);
public record RevokeInvitationResponseDto(Guid UserId, Guid InvitationId, string Status, DateTime UpdatedAt);
public record AdminUserListItemDto(Guid Id, string Email, string DisplayName, UserStatus Status, IReadOnlyList<string> Roles, ResponsibleAreaDto? ResponsibleArea, DateTime? LastLoginAt);
public record ResponsibleAreaDto(Guid Id, string Name);
public record ListUsersResponseDto(IReadOnlyList<AdminUserListItemDto> Items, int Page, int PageSize, int Total);
public record UpdateUserRequestDto(string? DisplayName = null, Guid? ResponsibleAreaId = null);
public record UpdateUserResponseDto(AdminUserDetailDto User, DateTime UpdatedAt);
public record SuspendUserRequestDto(string Reason);
public record SuspendUserResponseDto(Guid UserId, UserStatus NewStatus, DateTime UpdatedAt);
public record ReactivateUserRequestDto(string Reason);
public record ReactivateUserResponseDto(Guid UserId, UserStatus NewStatus, DateTime UpdatedAt);
public record DisableUserRequestDto(string Reason);
public record DisableUserResponseDto(Guid UserId, UserStatus NewStatus, DateTime UpdatedAt);
public record ChangeUserRolesRequestDto(IReadOnlyList<string> Roles, string Reason);
public record ChangeUserRolesResponseDto(Guid Id, IReadOnlyList<string> Roles, DateTime UpdatedAt);
