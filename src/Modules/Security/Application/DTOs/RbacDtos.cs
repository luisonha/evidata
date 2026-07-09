namespace Evidata.Modules.Security.Application.DTOs;

public record RoleDto(Guid Id, string Name, string? Description, bool IsSystemRole, IReadOnlyList<string> Permissions);
public record PermissionDto(Guid Id, string Name, string Resource, string Action, string? Description);
public record UserRoleAssignmentDto(Guid UserId, Guid RoleId, string RoleName, Guid TenantId, DateTime AssignedAt);
