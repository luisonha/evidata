using Evidata.Modules.ProcessingInventory.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;

public class ProcessingInventoryDbContext : DbContext
{
    public ProcessingInventoryDbContext(DbContextOptions<ProcessingInventoryDbContext> options)
        : base(options) { }

    public DbSet<ProcessingActivity> ProcessingActivities => Set<ProcessingActivity>();

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

            // ── Índices ───────────────────────────────────────────────────────
            e.HasIndex(a => new { a.TenantId, a.Name })
                .IsUnique()
                .HasDatabaseName("ix_processing_activities_tenant_name");

            e.HasIndex(a => new { a.TenantId, a.Status })
                .HasDatabaseName("ix_processing_activities_tenant_status");

            e.HasIndex(a => a.SupersedesId)
                .HasDatabaseName("ix_processing_activities_supersedes");
        });
    }
}
