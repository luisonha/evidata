using Evidata.Modules.GapManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Infrastructure.Persistence;

public class GapManagementDbContext : DbContext
{
    public GapManagementDbContext(DbContextOptions<GapManagementDbContext> options)
        : base(options) { }

    public DbSet<ComplianceGap> ComplianceGaps => Set<ComplianceGap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("gap");

        modelBuilder.Entity<ComplianceGap>(e =>
        {
            e.ToTable("compliance_gaps");
            e.HasKey(g => g.Id);

            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(g => g.SourceModule).HasColumnName("source_module").HasMaxLength(100).IsRequired();
            e.Property(g => g.SourceEntityId).HasColumnName("source_entity_id").IsRequired();
            e.Property(g => g.LegalObligationId).HasColumnName("legal_obligation_id");
            e.Property(g => g.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(g => g.Description).HasColumnName("description").IsRequired();
            e.Property(g => g.Severity).HasColumnName("severity")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(g => g.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(g => g.OwnerId).HasColumnName("owner_id");
            e.Property(g => g.DueAt).HasColumnName("due_at");
            e.Property(g => g.RiskAcceptanceJustification).HasColumnName("risk_acceptance_justification");
            e.Property(g => g.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(g => g.LastModifiedBy).HasColumnName("last_modified_by");
            e.Property(g => g.LastModifiedAt).HasColumnName("last_modified_at");
            e.Property(g => g.ClosedAt).HasColumnName("closed_at");
            e.Property(g => g.ClosedBy).HasColumnName("closed_by");

            e.Ignore(g => g.BlocksApproval);

            // ── Índices ───────────────────────────────────────────────────────
            e.HasIndex(g => new { g.TenantId, g.Status })
                .HasDatabaseName("ix_compliance_gaps_tenant_status");

            e.HasIndex(g => new { g.TenantId, g.Severity })
                .HasDatabaseName("ix_compliance_gaps_tenant_severity");

            e.HasIndex(g => new { g.TenantId, g.SourceModule, g.SourceEntityId })
                .HasDatabaseName("ix_compliance_gaps_source");

            e.HasIndex(g => g.OwnerId)
                .HasDatabaseName("ix_compliance_gaps_owner");
        });
    }
}
