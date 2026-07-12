using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class UserProfileRoleConfiguration : IEntityTypeConfiguration<UserProfileRole>
{
    public void Configure(EntityTypeBuilder<UserProfileRole> builder)
    {
        builder.ToTable("user_profile_roles", "identity");

        // Composite key: (UserProfileId, RoleId)
        builder.HasKey(x => new { x.UserProfileId, x.RoleId });

        builder.Property(x => x.UserProfileId)
            .IsRequired();

        builder.Property(x => x.RoleId)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.AssignedAt)
            .IsRequired();

        // Foreign key relationship with UserProfile - NO navigation property mapping here
        // since UserProfileRole has a private UserProfile property we don't want to track
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for querying
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => new { x.TenantId, x.UserProfileId });
        builder.HasIndex(x => new { x.TenantId, x.RoleId });
    }
}
