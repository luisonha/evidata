using Evidata.Modules.LegalKnowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;

public class LegalKnowledgeDbContext : DbContext
{
    public LegalKnowledgeDbContext(DbContextOptions<LegalKnowledgeDbContext> options) : base(options) { }

    public DbSet<LegalObligation> LegalObligations => Set<LegalObligation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("legal");

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

            // Code único globalmente
            e.HasIndex(o => o.Code).IsUnique().HasDatabaseName("ix_legal_obligations_code");
            e.HasIndex(o => o.Status).HasDatabaseName("ix_legal_obligations_status");
            e.HasIndex(o => o.LegalSourceName).HasDatabaseName("ix_legal_obligations_source");
        });
    }
}
