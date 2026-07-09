using Evidata.Modules.LegalKnowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;

public class LegalKnowledgeDbContext : DbContext
{
    public LegalKnowledgeDbContext(DbContextOptions<LegalKnowledgeDbContext> options) : base(options) { }

    public DbSet<LegalObligation> LegalObligations => Set<LegalObligation>();
    public DbSet<LegalSource> LegalSources => Set<LegalSource>();
    public DbSet<LegalSourceVersion> LegalSourceVersions => Set<LegalSourceVersion>();
    public DbSet<DataCategory> DataCategories => Set<DataCategory>();
    public DbSet<DataSubjectCategory> DataSubjectCategories => Set<DataSubjectCategory>();
    public DbSet<SecurityMeasure> SecurityMeasures => Set<SecurityMeasure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("legal");

        // ── LegalObligation ───────────────────────────────────────────────────
        modelBuilder.Entity<LegalObligation>(e =>
        {
            e.ToTable("legal_obligations");
            e.HasKey(o => o.Id);

            e.Property(o => o.Id).HasColumnName("id");
            e.Property(o => o.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            e.Property(o => o.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            e.Property(o => o.LegalText).HasColumnName("legal_text").IsRequired();
            e.Property(o => o.SourceArticle).HasColumnName("source_article").HasMaxLength(100);
            e.Property(o => o.LegalSourceName).HasColumnName("legal_source_name").HasMaxLength(255).IsRequired();
            e.Property(o => o.DeadlineDays).HasColumnName("deadline_days");
            e.Property(o => o.Frequency).HasColumnName("frequency")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(o => o.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(o => o.Notes).HasColumnName("notes");
            e.Property(o => o.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(o => o.UpdatedBy).HasColumnName("updated_by");
            e.Property(o => o.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(o => o.Code).IsUnique().HasDatabaseName("ix_legal_obligations_code");
            e.HasIndex(o => o.Status).HasDatabaseName("ix_legal_obligations_status");
            e.HasIndex(o => o.LegalSourceName).HasDatabaseName("ix_legal_obligations_source");
        });

        // ── LegalSource ───────────────────────────────────────────────────────
        modelBuilder.Entity<LegalSource>(e =>
        {
            e.ToTable("legal_sources");
            e.HasKey(s => s.Id);

            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            e.Property(s => s.Jurisdiction).HasColumnName("jurisdiction").HasMaxLength(10).IsRequired();
            e.Property(s => s.Type).HasColumnName("type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(s => s.PublishedAt).HasColumnName("published_at").IsRequired();
            e.Property(s => s.OfficialUrl).HasColumnName("official_url").HasMaxLength(2048);
            e.Property(s => s.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(s => s.Code).IsUnique().HasDatabaseName("ix_legal_sources_code");
            e.HasIndex(s => s.Jurisdiction).HasDatabaseName("ix_legal_sources_jurisdiction");
            e.HasIndex(s => s.IsActive).HasDatabaseName("ix_legal_sources_is_active");

            e.HasMany(s => s.Versions)
                .WithOne(v => v.LegalSource)
                .HasForeignKey(v => v.LegalSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Metadata.FindNavigation(nameof(LegalSource.Versions))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── LegalSourceVersion ────────────────────────────────────────────────
        modelBuilder.Entity<LegalSourceVersion>(e =>
        {
            e.ToTable("legal_source_versions");
            e.HasKey(v => v.Id);

            e.Property(v => v.Id).HasColumnName("id");
            e.Property(v => v.LegalSourceId).HasColumnName("legal_source_id").IsRequired();
            e.Property(v => v.VersionNumber).HasColumnName("version_number").IsRequired();
            e.Property(v => v.VersionTag).HasColumnName("version_tag").HasMaxLength(100).IsRequired();
            e.Property(v => v.EffectiveDate).HasColumnName("effective_date").IsRequired();
            e.Property(v => v.Summary).HasColumnName("summary").HasMaxLength(2000).IsRequired();
            e.Property(v => v.ChangelogUrl).HasColumnName("changelog_url").HasMaxLength(2048);
            e.Property(v => v.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();

            e.HasIndex(v => new { v.LegalSourceId, v.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ix_legal_source_versions_source_version");
            e.HasIndex(v => v.EffectiveDate).HasDatabaseName("ix_legal_source_versions_effective_date");
        });

        // ── DataCategory ──────────────────────────────────────────────────────
        modelBuilder.Entity<DataCategory>(e =>
        {
            e.ToTable("data_categories");
            e.HasKey(c => c.Id);

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(c => c.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(c => c.Description).HasColumnName("description");
            e.Property(c => c.IsSensitive).HasColumnName("is_sensitive").IsRequired();
            e.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(c => c.UpdatedBy).HasColumnName("updated_by");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            // Code único por scope (global o tenant)
            e.HasIndex(c => new { c.TenantId, c.Code })
                .IsUnique().HasDatabaseName("ix_data_categories_tenant_code");
            e.HasIndex(c => c.TenantId).HasDatabaseName("ix_data_categories_tenant_id");
        });

        // ── DataSubjectCategory ───────────────────────────────────────────────
        modelBuilder.Entity<DataSubjectCategory>(e =>
        {
            e.ToTable("data_subject_categories");
            e.HasKey(c => c.Id);

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(c => c.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(c => c.Description).HasColumnName("description");
            e.Property(c => c.RequiresSpecialSafeguards).HasColumnName("requires_special_safeguards").IsRequired();
            e.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(c => c.UpdatedBy).HasColumnName("updated_by");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(c => new { c.TenantId, c.Code })
                .IsUnique().HasDatabaseName("ix_data_subject_categories_tenant_code");
            e.HasIndex(c => c.TenantId).HasDatabaseName("ix_data_subject_categories_tenant_id");
        });

        // ── SecurityMeasure ───────────────────────────────────────────────────
        modelBuilder.Entity<SecurityMeasure>(e =>
        {
            e.ToTable("security_measures");
            e.HasKey(m => m.Id);

            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(m => m.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            e.Property(m => m.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(m => m.Description).HasColumnName("description");
            e.Property(m => m.MeasureType).HasColumnName("measure_type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(m => m.IsMandatory).HasColumnName("is_mandatory").IsRequired();
            e.Property(m => m.IsActive).HasColumnName("is_active").IsRequired();
            e.Property(m => m.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(m => m.UpdatedBy).HasColumnName("updated_by");
            e.Property(m => m.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(m => new { m.TenantId, m.Code })
                .IsUnique().HasDatabaseName("ix_security_measures_tenant_code");
            e.HasIndex(m => m.TenantId).HasDatabaseName("ix_security_measures_tenant_id");
            e.HasIndex(m => m.MeasureType).HasDatabaseName("ix_security_measures_type");
        });
    }
}
