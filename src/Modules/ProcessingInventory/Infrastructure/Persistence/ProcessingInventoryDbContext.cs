using Evidata.Modules.ProcessingInventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;

public class ProcessingInventoryDbContext : DbContext
{
    public ProcessingInventoryDbContext(DbContextOptions<ProcessingInventoryDbContext> options)
        : base(options) { }

    public DbSet<ProcessingActivity> ProcessingActivities => Set<ProcessingActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rat");

        modelBuilder.Entity<ProcessingActivity>(e =>
        {
            e.ToTable("processing_activities");
            e.HasKey(a => a.Id);

            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(a => a.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            e.Property(a => a.Description).HasColumnName("description");
            e.Property(a => a.Controller).HasColumnName("controller").HasMaxLength(300);
            e.Property(a => a.Department).HasColumnName("department").HasMaxLength(200);
            e.Property(a => a.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(a => a.Version).HasColumnName("version").IsRequired();
            e.Property(a => a.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by");
            e.Property(a => a.LastModifiedAt).HasColumnName("last_modified_at");
            e.Property(a => a.ApprovedBy).HasColumnName("approved_by");
            e.Property(a => a.ApprovedAt).HasColumnName("approved_at");
            e.Property(a => a.SupersedesId).HasColumnName("supersedes_id");

            e.Ignore(a => a.IsEditable); // computed property

            // Nombre único por tenant (case-insensitive en PG via índice funcional)
            e.HasIndex(a => new { a.TenantId, a.Name })
                .IsUnique()
                .HasDatabaseName("ix_processing_activities_tenant_name");

            e.HasIndex(a => new { a.TenantId, a.Status })
                .HasDatabaseName("ix_processing_activities_tenant_status");

            e.HasIndex(a => a.SupersedesId)
                .HasDatabaseName("ix_processing_activities_supersedes");
        });
    }
}
