namespace Evidata.Modules.Identity.Application.DTOs;

/// <summary>
/// Request DTO for local dev login without Entra ID.
/// Uses email or userId from the seed users.
/// </summary>
public record DevLoginRequestDto(
    string Email
);

/// <summary>
/// Response DTO for dev login — same shape as production login callback.
/// </summary>
public record DevLoginResponseDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<Guid> RoleIds,
    string SessionId,
    DateTime SessionExpiresAt
);

/// <summary>
/// Dev user profile for list endpoint — includes id and email for selection.
/// </summary>
public record DevUserProfileDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Status
);

/// <summary>
/// Response DTO for /dev/auth/users listing available dev profiles.
/// </summary>
public record DevUsersListResponseDto(
    IReadOnlyList<DevUserProfileDto> Users
);

/// <summary>
/// Request DTO for /dev/auth/expire-session — no parameters needed.
/// </summary>
public record ExpireSessionRequestDto;

/// <summary>
/// Response DTO for /dev/auth/expire-session.
/// </summary>
public record ExpireSessionResponseDto(
    bool Success,
    string Message
);

/// <summary>
/// Request DTO for /dev/auth/set-scenario — simulates specific scenarios.
/// </summary>
public record SetScenarioRequestDto(
    string Email,
    string Scenario  // "denied", "disabled", "no-tenant"
);

/// <summary>
/// Response DTO for /dev/auth/set-scenario.
/// </summary>
public record SetScenarioResponseDto(
    bool Success,
    string Message,
    string Scenario
);

/// <summary>
/// Response DTO for /dev/admin/users/reset-seed.
/// </summary>
public record ResetSeedResponseDto(
    bool Success,
    string Message,
    int UsersReset
);

/// <summary>
/// Request DTO for /dev/admin/users/create-scenario.
/// </summary>
public record CreateScenarioRequestDto(
    string ScenarioType,  // "pending-invitation", "suspended", "disabled", etc.
    string Email,
    string? DisplayName = null,
    string? Role = null
);

/// <summary>
/// Response DTO for /dev/admin/users/create-scenario.
/// </summary>
public record CreateScenarioResponseDto(
    bool Success,
    string Message,
    Guid UserId,
    string Email
);
