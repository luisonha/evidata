using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Commands;

public record InviteUserCommand(Guid TenantId, Guid CreatedByUserId, string Email, string DisplayName, string Role, Guid? ResponsibleAreaId = null, string? Message = null);
public record ResendInvitationCommand(Guid TenantId, Guid UserId, Guid ActionByUserId);
public record RevokeInvitationCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, string Reason);
public record UpdateUserCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, string? DisplayName = null, Guid? ResponsibleAreaId = null);
public record SuspendUserCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, string Reason);
public record ReactivateUserCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, string Reason);
public record DisableUserCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, string Reason);
public record ChangeUserRolesCommand(Guid TenantId, Guid UserId, Guid ActionByUserId, IReadOnlyList<string> Roles, string Reason);
