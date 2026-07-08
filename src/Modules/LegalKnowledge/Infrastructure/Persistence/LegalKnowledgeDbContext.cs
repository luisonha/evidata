using Evidata.Modules.LegalKnowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;

public class LegalKnowledgeDbContext : DbContext
{
    public LegalKnowledgeDbContext(DbContextOptions<LegalKnowledgeDbContext> options) : base(options) { }

    public DbSet<LegalObligation> LegalObligations => Set<LegalObligation>();
    public DbSet<LegalSource> LegalSources => Set<LegalSource>();
    public DbSet<LegalSourceVersion> LegalSourceVersions => Set<LegalSourceVersion>();

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
    }
}
