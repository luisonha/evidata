using Evidata.Worker.Outbox.Messaging;

namespace Evidata.Worker.Outbox.Persistence;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 50, CancellationToken ct = default);
    Task MarkSentAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
    Task IncrementRetryAsync(Guid id, CancellationToken ct = default);
}
