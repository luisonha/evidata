using Evidata.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Documents.Infrastructure.Persistence;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documents");

        // ── Document ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);

            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(d => d.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(512).IsRequired();
            e.Property(d => d.ContentType).HasColumnName("content_type").HasMaxLength(255).IsRequired();
            e.Property(d => d.SizeBytes).HasColumnName("size_bytes");
            e.Property(d => d.BlobPath).HasColumnName("blob_path").HasMaxLength(1024);
            e.Property(d => d.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(d => d.Description).HasColumnName("description").HasMaxLength(2000);
            e.Property(d => d.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(d => d.UpdatedBy).HasColumnName("updated_by");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.Property(d => d.DeletedAt).HasColumnName("deleted_at");
            e.Property(d => d.DeletedBy).HasColumnName("deleted_by");

            // Índices
            e.HasIndex(d => d.TenantId).HasDatabaseName("ix_documents_tenant_id");
            e.HasIndex(d => new { d.TenantId, d.Status }).HasDatabaseName("ix_documents_tenant_status");
            e.HasIndex(d => d.DeletedAt).HasDatabaseName("ix_documents_deleted_at");

            // Soft delete global filter
            e.HasQueryFilter(d => d.DeletedAt == null);

            // Navegación
            e.HasMany(d => d.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignorar backing field (no column)
            e.Metadata.FindNavigation(nameof(Document.Versions))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── DocumentVersion ───────────────────────────────────────────────────
        modelBuilder.Entity<DocumentVersion>(e =>
        {
            e.ToTable("document_versions");

            // Matching filter: evita warning por relación requerida con Document filtrado
            e.HasQueryFilter(v => v.Document!.DeletedAt == null);
            e.HasKey(v => v.Id);

            e.Property(v => v.Id).HasColumnName("id");
            e.Property(v => v.DocumentId).HasColumnName("document_id").IsRequired();
            e.Property(v => v.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(v => v.VersionNumber).HasColumnName("version_number").IsRequired();
            e.Property(v => v.BlobPath).HasColumnName("blob_path").HasMaxLength(1024).IsRequired();
            e.Property(v => v.SizeBytes).HasColumnName("size_bytes").IsRequired();
            e.Property(v => v.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();

            // Unicidad: un documento no puede tener dos versiones con el mismo número
            e.HasIndex(v => new { v.DocumentId, v.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ix_document_versions_doc_version");

            e.HasIndex(v => v.TenantId).HasDatabaseName("ix_document_versions_tenant_id");
        });
    }
}
