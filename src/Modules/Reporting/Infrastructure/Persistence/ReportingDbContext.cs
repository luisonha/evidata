using Evidata.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public DbSet<ReportJob> ReportJobs => Set<ReportJob>();

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
    }
}
