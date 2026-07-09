using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evidata.Modules.TenantManagement.Infrastructure.Persistence;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Status).HasConversion<string>();
        builder.OwnsOne(t => t.Settings, s =>
        {
            s.Property(x => x.TimeZone).HasMaxLength(50);
            s.Property(x => x.Locale).HasMaxLength(10);
        });
    }
}
