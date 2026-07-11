using Evidata.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public DbSet<ReportJob> ReportJobs => Set<ReportJob>();
    public DbSet<Export> Exports => Set<Export>();

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");

        modelBuilder.Entity<ReportJob>(e =>
        {
            e.ToTable("report_jobs");
            e.HasKey(j => j.Id);

            e.Property(j => j.Id).HasColumnName("id");
            e.Property(j => j.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(j => j.ReportType).HasColumnName("report_type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(j => j.Parameters).HasColumnName("parameters")
                .HasColumnType("jsonb").IsRequired();
            e.Property(j => j.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(j => j.RequestedBy).HasColumnName("requested_by").IsRequired();
            e.Property(j => j.ArtifactDocumentId).HasColumnName("artifact_document_id");
            e.Property(j => j.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            e.Property(j => j.RequestedAt).HasColumnName("requested_at").IsRequired();
            e.Property(j => j.StartedAt).HasColumnName("started_at");
            e.Property(j => j.CompletedAt).HasColumnName("completed_at");

            e.HasIndex(j => new { j.TenantId, j.Status })
                .HasDatabaseName("ix_report_jobs_tenant_status");
            e.HasIndex(j => new { j.TenantId, j.RequestedAt })
                .HasDatabaseName("ix_report_jobs_tenant_requested_at");

            e.Ignore(j => j.IsTerminal);
        });

        modelBuilder.Entity<Export>(e =>
        {
            e.ToTable("exports");
            e.HasKey(ex => ex.Id);

            e.Property(ex => ex.Id).HasColumnName("id");
            e.Property(ex => ex.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(ex => ex.ProcessingActivityId).HasColumnName("processing_activity_id").IsRequired();
            e.Property(ex => ex.ExportType).HasColumnName("export_type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(ex => ex.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(ex => ex.Version).HasColumnName("version").IsRequired();
            e.Property(ex => ex.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
            e.Property(ex => ex.ArtifactDocumentId).HasColumnName("artifact_document_id");
            e.Property(ex => ex.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();
            e.Property(ex => ex.RequestedAt).HasColumnName("requested_at").IsRequired();
            e.Property(ex => ex.GeneratedAt).HasColumnName("generated_at");
            e.Property(ex => ex.CorrelationId).HasColumnName("correlation_id").HasMaxLength(200).IsRequired();
            e.Property(ex => ex.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);

            // Warnings stored as JSONB array
            e.Property("_warnings")
                .HasColumnName("warnings")
                .HasColumnType("jsonb[]");

            e.HasIndex(ex => new { ex.TenantId, ex.ProcessingActivityId })
                .HasDatabaseName("ix_exports_tenant_activity");
            e.HasIndex(ex => new { ex.TenantId, ex.Status })
                .HasDatabaseName("ix_exports_tenant_status");
            e.HasIndex(ex => new { ex.ProcessingActivityId, ex.ExportType })
                .HasDatabaseName("ix_exports_activity_type");
            e.HasIndex(ex => ex.CorrelationId)
                .HasDatabaseName("ix_exports_correlation_id");

             e.Ignore(ex => ex.IsTerminal);
        });
    }
}