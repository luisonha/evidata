using Evidata.Modules.Evidence.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Infrastructure.Persistence;

public class EvidenceDbContext : DbContext
{
    public EvidenceDbContext(DbContextOptions<EvidenceDbContext> options) : base(options) { }

    public DbSet<Domain.Evidence> Evidences => Set<Domain.Evidence>();
    public DbSet<EvidenceAccessLog> EvidenceAccessLogs => Set<EvidenceAccessLog>();
    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();
    public DbSet<EvidencePackJob> EvidencePackJobs => Set<EvidencePackJob>();

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

            e.HasMany<EvidenceAccessLog>()
                .WithOne(l => l.Evidence)
                .HasForeignKey(l => l.EvidenceId)
                .OnDelete(DeleteBehavior.Restrict); // Preserve logs even if evidence is soft-deleted
        });

        // ── EvidenceAccessLog (append-only) ───────────────────────────────────
        modelBuilder.Entity<EvidenceAccessLog>(e =>
        {
            e.ToTable("evidence_access_logs");
            e.HasKey(l => l.Id);

            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(l => l.EvidenceId).HasColumnName("evidence_id").IsRequired();
            e.Property(l => l.AccessedBy).HasColumnName("accessed_by").IsRequired();
            e.Property(l => l.AccessedAt).HasColumnName("accessed_at").IsRequired();
            e.Property(l => l.Reason).HasColumnName("reason").HasMaxLength(2000);
            e.Property(l => l.ClientIp).HasColumnName("client_ip").HasMaxLength(64);
            e.Property(l => l.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            e.Property(l => l.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            e.Property(l => l.SensitivityAtAccess).HasColumnName("sensitivity_at_access")
                .HasConversion<string>().HasMaxLength(50).IsRequired();

            e.HasIndex(l => l.EvidenceId).HasDatabaseName("ix_evidence_access_logs_evidence_id");
            e.HasIndex(l => new { l.TenantId, l.AccessedAt }).HasDatabaseName("ix_evidence_access_logs_tenant_date");
            e.HasIndex(l => l.AccessedBy).HasDatabaseName("ix_evidence_access_logs_user");
        });

        // ── EvidenceLink ──────────────────────────────────────────────────────
        modelBuilder.Entity<EvidenceLink>(e =>
        {
            e.ToTable("evidence_links");
            e.HasKey(l => l.Id);

            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(l => l.EvidenceId).HasColumnName("evidence_id").IsRequired();
            e.Property(l => l.LinkedEntityType).HasColumnName("linked_entity_type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(l => l.LinkedEntityId).HasColumnName("linked_entity_id").IsRequired();
            e.Property(l => l.Note).HasColumnName("note").HasMaxLength(1000);
            e.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(l => l.DeletedAt).HasColumnName("deleted_at");
            e.Property(l => l.DeletedBy).HasColumnName("deleted_by");

            e.Ignore(l => l.IsActive); // computed property

            e.HasOne(l => l.Evidence)
                .WithMany()
                .HasForeignKey(l => l.EvidenceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(l => new { l.TenantId, l.EvidenceId })
                .HasDatabaseName("ix_evidence_links_tenant_evidence");
            e.HasIndex(l => new { l.EvidenceId, l.LinkedEntityType, l.LinkedEntityId, l.DeletedAt })
                .HasDatabaseName("ix_evidence_links_dedup")
                .HasFilter("deleted_at IS NULL");
        });

        // ── EvidencePackJob ───────────────────────────────────────────────────
        modelBuilder.Entity<EvidencePackJob>(e =>
        {
            e.ToTable("evidence_pack_jobs");
            e.HasKey(j => j.Id);

            e.Property(j => j.Id).HasColumnName("id");
            e.Property(j => j.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(j => j.RequestedBy).HasColumnName("requested_by").IsRequired();
            e.Property(j => j.RequestedAt).HasColumnName("requested_at").IsRequired();
            e.Property(j => j.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(j => j.ResultBlobPath).HasColumnName("result_blob_path").HasMaxLength(1000);
            e.Property(j => j.ErrorMessage).HasColumnName("error_message").HasMaxLength(4000);
            e.Property(j => j.StartedAt).HasColumnName("started_at");
            e.Property(j => j.CompletedAt).HasColumnName("completed_at");
            e.Property(j => j.ExpiresAt).HasColumnName("expires_at");

            // EvidenceIds stored as JSON array
            e.Property<List<Guid>>("_evidenceIds")
                .HasColumnName("evidence_ids")
                .HasColumnType("jsonb")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasIndex(j => j.TenantId).HasDatabaseName("ix_evidence_pack_jobs_tenant");
            e.HasIndex(j => new { j.TenantId, j.Status }).HasDatabaseName("ix_evidence_pack_jobs_tenant_status");
            e.HasIndex(j => j.ExpiresAt).HasDatabaseName("ix_evidence_pack_jobs_expires");
        });
    }
}
