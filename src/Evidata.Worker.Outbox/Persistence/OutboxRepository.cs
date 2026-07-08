using Evidata.Worker.Outbox.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Worker.Outbox.Persistence;

public class OutboxRepository : IOutboxRepository, IOutboxWriter
{
    private const int MaxRetries = 5;
    private readonly OutboxDbContext _ctx;

    public OutboxRepository(OutboxDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 50, CancellationToken ct = default)
        => await _ctx.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending && x.RetryCount < MaxRetries)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task MarkSentAsync(Guid id, CancellationToken ct = default)
    {
        var msg = await _ctx.OutboxMessages.FindAsync([id], ct);
        if (msg is null) return;
        msg.Status = OutboxMessageStatus.Sent;
        msg.ProcessedAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var msg = await _ctx.OutboxMessages.FindAsync([id], ct);
        if (msg is null) return;
        msg.Status = OutboxMessageStatus.Dead;
        msg.ErrorMessage = error;
        msg.ProcessedAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task IncrementRetryAsync(Guid id, CancellationToken ct = default)
    {
        var msg = await _ctx.OutboxMessages.FindAsync([id], ct);
        if (msg is null) return;
        msg.RetryCount++;
        if (msg.RetryCount >= MaxRetries)
        {
            msg.Status = OutboxMessageStatus.Dead;
            msg.ErrorMessage = $"Max retries ({MaxRetries}) alcanzado.";
        }
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task EnqueueAsync(
        string tenantId, string destination, string messageType,
        string payload, string? correlationId = null, CancellationToken ct = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Destination = destination,
            MessageType = messageType,
            Payload = payload,
            CorrelationId = correlationId,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _ctx.OutboxMessages.Add(message);
        await _ctx.SaveChangesAsync(ct);
    }
}
