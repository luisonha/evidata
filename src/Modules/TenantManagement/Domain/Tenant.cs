namespace Evidata.Modules.TenantManagement.Domain;

public class Tenant
{
    public Guid Id { get; private set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public TenantSettings Settings { get; set; } = new();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    private Tenant() { }

    public static Tenant Create(string slug, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant { Id = Guid.NewGuid(), Slug = slug.ToLowerInvariant(), Name = name };
    }
}
