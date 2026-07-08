using Evidata.Modules.Evidence.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Infrastructure.Persistence;

public class EvidenceDbContext : DbContext
{
    public EvidenceDbContext(DbContextOptions<EvidenceDbContext> options) : base(options) { }

    public DbSet<Domain.Evidence> Evidences => Set<Domain.Evidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("evidence");

        modelBuilder.Entity<Domain.Evidence>(e =>
        {
            e.ToTable("evidences");
            e.HasKey(ev => ev.Id);

            e.Property(ev => ev.Id).HasColumnName("id");
            e.Property(ev => ev.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(ev => ev.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            e.Property(ev => ev.Description).HasColumnName("description");
            e.Property(ev => ev.Type).HasColumnName("type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(ev => ev.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(ev => ev.Sensitivity).HasColumnName("sensitivity")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(ev => ev.BlobPath).HasColumnName("blob_path").HasMaxLength(1024);
            e.Property(ev => ev.ContentType).HasColumnName("content_type").HasMaxLength(255);
            e.Property(ev => ev.SizeBytes).HasColumnName("size_bytes");
            e.Property(ev => ev.SupersedesEvidenceId).HasColumnName("supersedes_evidence_id");
            e.Property(ev => ev.Tags).HasColumnName("tags").HasMaxLength(1000);
            e.Property(ev => ev.DeletionReason).HasColumnName("deletion_reason").HasMaxLength(2000);
            e.Property(ev => ev.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(ev => ev.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(ev => ev.UpdatedBy).HasColumnName("updated_by");
            e.Property(ev => ev.UpdatedAt).HasColumnName("updated_at");

            // Soft delete: excluir Deleted del filtro global
            e.HasQueryFilter(ev => ev.Status != EvidenceStatus.Deleted);

            // Índices
            e.HasIndex(ev => ev.TenantId).HasDatabaseName("ix_evidences_tenant_id");
            e.HasIndex(ev => new { ev.TenantId, ev.Status }).HasDatabaseName("ix_evidences_tenant_status");
            e.HasIndex(ev => new { ev.TenantId, ev.Type }).HasDatabaseName("ix_evidences_tenant_type");
            e.HasIndex(ev => ev.Sensitivity).HasDatabaseName("ix_evidences_sensitivity");
            e.HasIndex(ev => ev.SupersedesEvidenceId).HasDatabaseName("ix_evidences_supersedes");
        });
    }
}
