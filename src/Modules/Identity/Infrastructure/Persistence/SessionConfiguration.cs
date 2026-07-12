using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions", "identity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(128);  // Opaque session ID (base64 encoded random bytes)

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.RolesSnapshot)
            .IsRequired()
            .HasMaxLength(2000);  // Comma-separated role IDs

        builder.Property(x => x.PermissionsVersion)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.LastAccessedAt)
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .IsRequired(false);

        // Indices for fast lookups and filtering
        builder.HasIndex(x => x.Id)
            .IsUnique()
            .HasDatabaseName("idx_sessions_id_unique");

        // (TenantId, UserId) for finding all sessions of a user to revoke on suspend/disable
        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("idx_sessions_tenant_user");

        // ExpiresAt for cleanup queries (delete expired sessions)
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_sessions_expires_at");

        // RevokedAt for finding revoked sessions (for cleanup or auditing)
        builder.HasIndex(x => x.RevokedAt)
            .HasDatabaseName("idx_sessions_revoked_at");
    }
}
