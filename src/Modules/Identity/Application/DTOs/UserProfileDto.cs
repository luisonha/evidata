namespace Evidata.Modules.Identity.Application.DTOs;

public record UserProfileDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    string Provider,
    bool IsActive
);
