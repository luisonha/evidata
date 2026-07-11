using Evidata.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext
{
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewRequirement> ReviewRequirements => Set<ReviewRequirement>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();

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
            e.Property(r => r.ReviewDomain).HasColumnName("review_domain").HasDefaultValue(0).IsRequired();
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

        modelBuilder.Entity<ReviewRequirement>(e =>
        {
            e.ToTable("review_requirements");
            e.HasKey(r => r.Id);

            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(r => r.ReviewType).HasColumnName("review_type")
                .HasConversion<int>().IsRequired();
            e.Property(r => r.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
            e.Property(r => r.IsRequired).HasColumnName("is_required").IsRequired();
            e.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(r => r.ModifiedAt).HasColumnName("modified_at").IsRequired();
            e.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(r => r.ModifiedBy).HasColumnName("modified_by").IsRequired();

            e.HasIndex(r => new { r.TenantId, r.EntityType, r.ReviewType })
                .HasDatabaseName("ix_review_requirements_tenant_entity_type")
                .IsUnique();
            e.HasIndex(r => r.TenantId).HasDatabaseName("ix_review_requirements_tenant");
        });

        modelBuilder.Entity<WorkflowTask>(e =>
        {
            e.ToTable("workflow_tasks");
            e.HasKey(t => t.Id);

            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(t => t.TargetModule).HasColumnName("target_module").HasMaxLength(100).IsRequired();
            e.Property(t => t.TargetEntityType).HasColumnName("target_entity_type").HasMaxLength(100).IsRequired();
            e.Property(t => t.TargetEntityId).HasColumnName("target_entity_id").IsRequired();
            e.Property(t => t.TaskType).HasColumnName("task_type")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(t => t.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(t => t.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(t => t.Description).HasColumnName("description").HasMaxLength(2000);
            e.Property(t => t.AssignedTo).HasColumnName("assigned_to").IsRequired();
            e.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(t => t.DueAt).HasColumnName("due_at");
            e.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(t => t.StartedAt).HasColumnName("started_at");
            e.Property(t => t.CompletedAt).HasColumnName("completed_at");

            e.HasIndex(t => new { t.TenantId, t.Status }).HasDatabaseName("ix_workflow_tasks_tenant_status");
            e.HasIndex(t => new { t.TenantId, t.AssignedTo }).HasDatabaseName("ix_workflow_tasks_tenant_assignee");
            e.HasIndex(t => new { t.TenantId, t.TargetEntityId }).HasDatabaseName("ix_workflow_tasks_tenant_entity");
        });
    }
}
