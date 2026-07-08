using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evidata.Modules.Audit.Infrastructure.Persistence;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "audit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Resource).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Details).HasMaxLength(4096);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.Severity).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        builder.HasIndex(x => new { x.TenantId, x.Resource, x.ResourceId });
    }
}
