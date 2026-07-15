namespace Evidata.Modules.Identity.Application.DTOs;

/// <summary>
/// Response DTO for login callback after successful authentication.
/// Contains user profile and session information.
/// </summary>
public record LoginCallbackResponseDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<Guid> RoleIds,
    string SessionId,
    DateTime SessionExpiresAt
);

/// <summary>
/// Response DTO for logout operation.
/// </summary>
public record LogoutResponseDto(
    bool Success,
    string Message
);

/// <summary>
/// Response DTO for session status endpoint.
/// </summary>
public record SessionStatusDto(
    bool IsAuthenticated,
    Guid? UserId,
    Guid? TenantId,
    DateTime? ExpiresAt
);

/// <summary>
/// Response DTO for /api/me endpoint.
/// </summary>
public record UserProfileResponseDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<Guid> RoleIds
);

/// <summary>
/// Response DTO for /api/permissions endpoint.
/// </summary>
public record UserPermissionsResponseDto(
    IReadOnlyList<string> Permissions
);

/// <summary>
/// Response DTO for CSRF token endpoint.
/// </summary>
public record CsrfTokenResponseDto(
    string Token
);
