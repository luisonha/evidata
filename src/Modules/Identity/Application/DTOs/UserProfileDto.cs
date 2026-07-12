using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.DTOs;

public record UserProfileDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    string Provider,
    UserStatus Status
)
{
    /// <summary>
    /// Backward compatibility property for existing code that checks if user is active.
    /// </summary>
    public bool IsActive => Status == UserStatus.Active;
}
