using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Infrastructure.Persistence;

public class GapManagementDbContext : DbContext
{
    public GapManagementDbContext(DbContextOptions<GapManagementDbContext> options)
        : base(options) { }

    public DbSet<ComplianceGap> ComplianceGaps => Set<ComplianceGap>();
    public DbSet<GapRule> GapRules => Set<GapRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("gap");

        // ── GapRule ────────────────────────────────────────────────────────────
        modelBuilder.Entity<GapRule>(e =>
        {
            e.ToTable("gap_rules");
            e.HasKey(r => r.Id);

            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(r => r.RuleCode).HasColumnName("rule_code").HasMaxLength(100).IsRequired();
            e.Property(r => r.Description).HasColumnName("description").IsRequired();
            e.Property(r => r.Severity).HasColumnName("severity")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(r => r.BlocksApproval).HasColumnName("blocks_approval").IsRequired();
            e.Property(r => r.TestFixtureName).HasColumnName("test_fixture_name").HasMaxLength(100).IsRequired();
            e.Property(r => r.IsFullyImplemented).HasColumnName("is_fully_implemented").IsRequired();
            e.Property(r => r.ImplementationNotes).HasColumnName("implementation_notes");
            e.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(r => r.LastModifiedBy).HasColumnName("last_modified_by");
            e.Property(r => r.LastModifiedAt).HasColumnName("last_modified_at");

            // ── Índices ────────────────────────────────────────────────────────
            e.HasIndex(r => new { r.TenantId, r.RuleCode })
                .HasDatabaseName("ix_gap_rules_tenant_code")
                .IsUnique();

            e.HasIndex(r => r.RuleCode)
                .HasDatabaseName("ix_gap_rules_code");
        });

        // ── ComplianceGap ──────────────────────────────────────────────────────
        modelBuilder.Entity<ComplianceGap>(e =>
        {
            e.ToTable("compliance_gaps");
            e.HasKey(g => g.Id);

            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(g => g.SourceModule).HasColumnName("source_module").HasMaxLength(100).IsRequired();
            e.Property(g => g.SourceEntityId).HasColumnName("source_entity_id").IsRequired();
            e.Property(g => g.LegalObligationId).HasColumnName("legal_obligation_id");
            e.Property(g => g.GapRuleId).HasColumnName("gap_rule_id");
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

            // ── Índices ────────────────────────────────────────────────────────
            e.HasIndex(g => new { g.TenantId, g.Status })
                .HasDatabaseName("ix_compliance_gaps_tenant_status");

            e.HasIndex(g => new { g.TenantId, g.Severity })
                .HasDatabaseName("ix_compliance_gaps_tenant_severity");

            e.HasIndex(g => new { g.TenantId, g.SourceModule, g.SourceEntityId })
                .HasDatabaseName("ix_compliance_gaps_source");

            e.HasIndex(g => g.OwnerId)
                .HasDatabaseName("ix_compliance_gaps_owner");

            e.HasIndex(g => g.GapRuleId)
                .HasDatabaseName("ix_compliance_gaps_rule");
        });
    }
}
