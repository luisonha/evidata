using Evidata.Modules.ProcessingInventory.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;

public class ProcessingInventoryDbContext : DbContext
{
    public ProcessingInventoryDbContext(DbContextOptions<ProcessingInventoryDbContext> options)
        : base(options) { }

    public DbSet<ProcessingActivity> ProcessingActivities => Set<ProcessingActivity>();
    public DbSet<ProcessingActivitySnapshot> ProcessingActivitySnapshots => Set<ProcessingActivitySnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rat");

        modelBuilder.Entity<ProcessingActivity>(e =>
        {
            e.ToTable("processing_activities");
            e.HasKey(a => a.Id);

            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(a => a.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(a => a.Description).HasColumnName("description");
            e.Property(a => a.Controller).HasColumnName("controller").HasMaxLength(300);
            e.Property(a => a.Department).HasColumnName("department").HasMaxLength(200);
            e.Property(a => a.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(a => a.Version).HasColumnName("version").IsRequired();
            e.Property(a => a.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by");
            e.Property(a => a.LastModifiedAt).HasColumnName("last_modified_at");
            e.Property(a => a.ApprovedBy).HasColumnName("approved_by");
            e.Property(a => a.ApprovedAt).HasColumnName("approved_at");
            e.Property(a => a.SupersedesId).HasColumnName("supersedes_id");

            e.Ignore(a => a.IsEditable);

            // ── PurposeSection (owned type) ───────────────────────────────────
            e.OwnsOne(a => a.Purpose, p =>
            {
                p.Property(s => s.Purpose).HasColumnName("purpose_text");
                p.Property(s => s.LegalBasis).HasColumnName("legal_basis")
                    .HasConversion<string>().HasMaxLength(50);
                p.Property(s => s.LegalBasisJustification).HasColumnName("legal_basis_justification");
            });

            // ── DataCategories (JSONB) ────────────────────────────────────────
            e.Property<List<DataCategoryEntry>>("_dataCategories")
                .HasColumnName("data_categories")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<DataCategoryEntry>>(v, (JsonSerializerOptions?)null) ?? new());

            e.Ignore(a => a.DataCategories); // exposed via read-only property

            // ── DataSubjects (JSONB) ──────────────────────────────────────────
            e.Property<List<DataSubjectEntry>>("_dataSubjects")
                .HasColumnName("data_subjects")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<DataSubjectEntry>>(v, (JsonSerializerOptions?)null) ?? new());

            e.Ignore(a => a.DataSubjects);

            // ── Systems (JSONB) ───────────────────────────────────────────────
            e.Property<List<SystemEntry>>("_systems")
                .HasColumnName("systems")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SystemEntry>>(v, (JsonSerializerOptions?)null) ?? new());
            e.Ignore(a => a.Systems);

            // ── Suppliers (JSONB) ─────────────────────────────────────────────
            e.Property<List<SupplierEntry>>("_suppliers")
                .HasColumnName("suppliers")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SupplierEntry>>(v, (JsonSerializerOptions?)null) ?? new());
            e.Ignore(a => a.Suppliers);

            // ── RetentionSection (owned type) ─────────────────────────────────
            e.OwnsOne(a => a.Retention, r =>
            {
                r.Property(s => s.PeriodDescription).HasColumnName("retention_period_description");
                r.Property(s => s.RetentionMonths).HasColumnName("retention_months");
                r.Property(s => s.LegalJustification).HasColumnName("retention_legal_justification");
            });

            // ── SecurityMeasures (JSONB) ──────────────────────────────────────
            e.Property<List<SecurityMeasureEntry>>("_securityMeasures")
                .HasColumnName("security_measures")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SecurityMeasureEntry>>(v, (JsonSerializerOptions?)null) ?? new());
            e.Ignore(a => a.SecurityMeasures);

            // ── RiskFlags (owned type — columnas booleanas) ───────────────────
            e.OwnsOne(a => a.Flags, f =>
            {
                f.Property(r => r.SensitiveData).HasColumnName("flag_sensitive_data");
                f.Property(r => r.ChildrenData).HasColumnName("flag_children_data");
                f.Property(r => r.BiometricData).HasColumnName("flag_biometric_data");
                f.Property(r => r.InternationalTransfer).HasColumnName("flag_international_transfer");
                f.Property(r => r.AutomatedDecision).HasColumnName("flag_automated_decision");
                f.Property(r => r.MissingLegalBasisEvidence).HasColumnName("flag_missing_legal_basis_evidence");
                f.Property(r => r.MissingRetention).HasColumnName("flag_missing_retention");
                f.Property(r => r.MissingSecurityMeasures).HasColumnName("flag_missing_security_measures");
                f.Property(r => r.CriticalGapOpen).HasColumnName("flag_critical_gap_open");
                f.Ignore(r => r.RequiresEnhancedReview);
                f.Ignore(r => r.BlocksApproval);
                f.Ignore(r => r.IsClean);
            });

            e.Property(a => a.HasInternationalTransfer).HasColumnName("has_international_transfer");
            e.Property(a => a.HasAutomatedDecision).HasColumnName("has_automated_decision");

            // ── Índices ───────────────────────────────────────────────────────
            e.HasIndex(a => new { a.TenantId, a.Name })
                .IsUnique()
                .HasDatabaseName("ix_processing_activities_tenant_name");

            e.HasIndex(a => new { a.TenantId, a.Status })
                .HasDatabaseName("ix_processing_activities_tenant_status");

            e.HasIndex(a => a.SupersedesId)
                .HasDatabaseName("ix_processing_activities_supersedes");
        });

        // ── ProcessingActivitySnapshot ────────────────────────────────────────
        modelBuilder.Entity<ProcessingActivitySnapshot>(e =>
        {
            e.ToTable("processing_activity_snapshots");
            e.HasKey(s => s.Id);

            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(s => s.ActivityId).HasColumnName("activity_id").IsRequired();
            e.Property(s => s.Version).HasColumnName("version").IsRequired();
            e.Property(s => s.ApprovedBy).HasColumnName("approved_by").IsRequired();
            e.Property(s => s.ApprovedAt).HasColumnName("approved_at").IsRequired();
            e.Property(s => s.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();

            e.HasIndex(s => s.ActivityId)
                .HasDatabaseName("ix_pat_snapshots_activity_id");

            e.HasIndex(s => new { s.TenantId, s.ActivityId, s.Version })
                .IsUnique()
                .HasDatabaseName("ix_pat_snapshots_tenant_activity_version");
        });
    }
}
