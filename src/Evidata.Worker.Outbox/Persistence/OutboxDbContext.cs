using Evidata.Worker.Outbox.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Worker.Outbox.Persistence;

public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("outbox");

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages", "outbox");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            b.Property(x => x.Destination).IsRequired().HasMaxLength(128);
            b.Property(x => x.MessageType).IsRequired().HasMaxLength(256);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(128);
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.RetryCount).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(2048);

            b.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        base.OnModelCreating(modelBuilder);
    }
}
