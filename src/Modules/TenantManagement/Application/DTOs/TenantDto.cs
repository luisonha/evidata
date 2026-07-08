namespace Evidata.Modules.TenantManagement.Application.DTOs;
public record TenantDto(
    Guid Id,
    string Slug,
    string Name,
    string Status,
    string? TimeZone,
    string? Locale,
    int MaxUsers,
    bool MfaRequired,
    DateTimeOffset CreatedAt);
