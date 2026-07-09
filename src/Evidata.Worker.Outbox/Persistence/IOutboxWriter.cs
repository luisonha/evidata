using Evidata.Worker.Outbox.Messaging;

namespace Evidata.Worker.Outbox.Persistence;

/// <summary>
/// Interfaz para que los módulos de dominio encolen mensajes en el Outbox.
/// Se inyecta en los command handlers para escribir mensajes en la misma unidad de trabajo.
/// </summary>
public interface IOutboxWriter
{
    Task EnqueueAsync(
        string tenantId,
        string destination,
        string messageType,
        string payload,
        string? correlationId = null,
        CancellationToken ct = default);
}
