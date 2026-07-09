namespace Evidata.Modules.TenantManagement.Domain;
public class TenantSettings
{
    public string? TimeZone { get; set; } = "UTC";
    public string? Locale { get; set; } = "es-CL";
    public int MaxUsers { get; set; } = 10;
    public bool MfaRequired { get; set; } = false;
}
