namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Usuarios semilla para desarrollo local.
/// Usar estos GUIDs en los headers X-Evidata-Dev-User y X-Evidata-Dev-Tenant.
/// </summary>
public static class LocalDevSeedUsers
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid RegularUserId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    public static IReadOnlyList<SeedUser> All =>
    [
        new(AdminUserId, DefaultTenantId, "admin@localdev.evidata", "Admin LocalDev"),
        new(RegularUserId, DefaultTenantId, "user@localdev.evidata", "User LocalDev")
    ];
}

public record SeedUser(Guid UserId, Guid TenantId, string Email, string DisplayName);
