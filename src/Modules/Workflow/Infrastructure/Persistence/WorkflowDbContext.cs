using Evidata.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext
{
    public DbSet<Review> Reviews => Set<Review>();

    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workflow");

        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(r => r.Id);

            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(r => r.TargetModule).HasColumnName("target_module").HasMaxLength(100).IsRequired();
            e.Property(r => r.TargetEntityType).HasColumnName("target_entity_type").HasMaxLength(100).IsRequired();
            e.Property(r => r.TargetEntityId).HasColumnName("target_entity_id").IsRequired();
            e.Property(r => r.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(r => r.RequestedBy).HasColumnName("requested_by").IsRequired();
            e.Property(r => r.ReviewerId).HasColumnName("reviewer_id");
            e.Property(r => r.Comments).HasColumnName("comments").HasMaxLength(2000);
            e.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(r => r.StartedAt).HasColumnName("started_at");
            e.Property(r => r.CompletedAt).HasColumnName("completed_at");

            e.HasIndex(r => new { r.TenantId, r.Status }).HasDatabaseName("ix_reviews_tenant_status");
            e.HasIndex(r => new { r.TenantId, r.TargetEntityId }).HasDatabaseName("ix_reviews_tenant_entity");
        });
    }
}
