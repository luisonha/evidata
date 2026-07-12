using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", "identity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Ignore the UserProfileRoles collection property - it's only for domain logic
        // The actual many-to-many relationship is managed by the join table UserProfileRole
        builder.Ignore(x => x.UserProfileRoles);

        builder.HasIndex(x => new { x.ExternalId, x.Provider, x.TenantId })
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
    }
}
