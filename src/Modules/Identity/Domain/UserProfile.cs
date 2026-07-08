using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Domain;

public class UserProfile : ITenantScoped
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(string externalId, string provider, string email, string displayName, Guid tenantId)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Provider = provider,
            Email = email,
            DisplayName = displayName,
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
