using Evidata.Modules.TenantManagement.Domain;

namespace Evidata.Modules.TenantManagement.Application.DTOs;

public static class TenantMappings
{
    public static TenantDto ToDto(this Tenant tenant) => new(
        tenant.Id,
        tenant.Slug,
        tenant.Name,
        tenant.Status.ToString(),
        tenant.Settings.TimeZone,
        tenant.Settings.Locale,
        tenant.Settings.MaxUsers,
        tenant.Settings.MfaRequired,
        tenant.CreatedAt);
}
