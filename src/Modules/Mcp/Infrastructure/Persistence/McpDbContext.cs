using Evidata.Modules.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Mcp.Infrastructure.Persistence;

public sealed class McpDbContext : DbContext
{
    public DbSet<McpInteraction> McpInteractions => Set<McpInteraction>();
    public DbSet<McpCitation> McpCitations => Set<McpCitation>();
    public DbSet<McpReviewTask> McpReviewTasks => Set<McpReviewTask>();
    public DbSet<McpFeedback> McpFeedbacks => Set<McpFeedback>();

    public McpDbContext(DbContextOptions<McpDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("mcp");

        modelBuilder.Entity<McpInteraction>(e =>
        {
            e.ToTable("mcp_interactions");
            e.HasKey(i => i.Id);

            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(i => i.UserId).HasColumnName("user_id").IsRequired();
            e.Property(i => i.Question).HasColumnName("question").HasMaxLength(4000).IsRequired();
            e.Property(i => i.Answer).HasColumnName("answer").HasMaxLength(8000).IsRequired();
            e.Property(i => i.RiskLevel).HasColumnName("risk_level")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(i => i.UsedTenantContext).HasColumnName("used_tenant_context").IsRequired();
            e.Property(i => i.RequiresHumanReview).HasColumnName("requires_human_review").IsRequired();
            e.Property(i => i.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(i => i.OccurredAt).HasColumnName("occurred_at").IsRequired();

            e.HasMany(i => i.Citations)
                .WithOne()
                .HasForeignKey(c => c.InteractionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Metadata.FindNavigation(nameof(McpInteraction.Citations))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            e.HasIndex(i => new { i.TenantId, i.Status }).HasDatabaseName("ix_mcp_interactions_tenant_status");
            e.HasIndex(i => new { i.TenantId, i.OccurredAt }).HasDatabaseName("ix_mcp_interactions_tenant_time");
        });

        modelBuilder.Entity<McpCitation>(e =>
        {
            e.ToTable("mcp_citations");
            e.HasKey(c => c.Id);

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.InteractionId).HasColumnName("interaction_id").IsRequired();
            e.Property(c => c.SourceType).HasColumnName("source_type")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(c => c.SourceId).HasColumnName("source_id").HasMaxLength(500).IsRequired();
            e.Property(c => c.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
            e.Property(c => c.Fragment).HasColumnName("fragment").HasMaxLength(2000).IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

            e.HasIndex(c => c.InteractionId).HasDatabaseName("ix_mcp_citations_interaction_id");
        });

        modelBuilder.Entity<McpReviewTask>(e =>
        {
            e.ToTable("mcp_review_tasks");
            e.HasKey(t => t.Id);

            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.InteractionId).HasColumnName("interaction_id").IsRequired();
            e.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(t => t.AssignedTo).HasColumnName("assigned_to");
            e.Property(t => t.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(t => t.ReviewNotes).HasColumnName("review_notes").HasMaxLength(2000);
            e.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(t => t.StartedAt).HasColumnName("started_at");
            e.Property(t => t.CompletedAt).HasColumnName("completed_at");

            e.HasIndex(t => new { t.TenantId, t.Status }).HasDatabaseName("ix_mcp_review_tasks_tenant_status");
            e.HasIndex(t => t.InteractionId).HasDatabaseName("ix_mcp_review_tasks_interaction_id");
        });

        modelBuilder.Entity<McpFeedback>(e =>
        {
            e.ToTable("mcp_feedbacks");
            e.HasKey(f => f.Id);

            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.InteractionId).HasColumnName("interaction_id").IsRequired();
            e.Property(f => f.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
            e.Property(f => f.Rating).HasColumnName("rating")
                .HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(f => f.Comment).HasColumnName("comment").HasMaxLength(1000);
            e.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

            e.HasIndex(f => f.InteractionId).HasDatabaseName("ix_mcp_feedbacks_interaction_id");
            e.HasIndex(f => new { f.TenantId, f.UserId }).HasDatabaseName("ix_mcp_feedbacks_tenant_user");
        });
    }
}
