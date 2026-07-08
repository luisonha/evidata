using Evidata.Modules.Search.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Search.Infrastructure.Persistence;

public sealed class SearchDbContext : DbContext
{
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("search");

        modelBuilder.Entity<SearchQueryLog>(e =>
        {
            e.ToTable("search_query_logs");
            e.HasKey(l => l.Id);

            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(l => l.UserId).HasColumnName("user_id").IsRequired();
            e.Property(l => l.Query).HasColumnName("query").HasMaxLength(1000).IsRequired();
            e.Property(l => l.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(l => l.FiltersJson).HasColumnName("filters_json").HasColumnType("jsonb");
            e.Property(l => l.ResultCount).HasColumnName("result_count").IsRequired();
            e.Property(l => l.HadSelection).HasColumnName("had_selection");
            e.Property(l => l.OccurredAt).HasColumnName("occurred_at").IsRequired();

            e.HasIndex(l => new { l.TenantId, l.UserId, l.OccurredAt })
                .HasDatabaseName("ix_search_query_logs_tenant_user_time");
            e.HasIndex(l => new { l.TenantId, l.OccurredAt })
                .HasDatabaseName("ix_search_query_logs_tenant_time");
        });
    }
}
