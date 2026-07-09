namespace Evidata.Worker.Outbox.Persistence;

/// <summary>
/// Implementación no-operativa de <see cref="IOutboxWriter"/>.
/// Usar en contextos donde el módulo de GapManagement es necesario solo para
/// lectura (ej: functions de reporting) y el Outbox real no está disponible.
/// </summary>
public sealed class NullOutboxWriter : IOutboxWriter
{
    public Task EnqueueAsync(
        string tenantId,
        string destination,
        string messageType,
        string payload,
        string? correlationId = null,
        CancellationToken ct = default) => Task.CompletedTask;
}
