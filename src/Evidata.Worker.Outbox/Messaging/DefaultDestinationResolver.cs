namespace Evidata.Worker.Outbox.Messaging;

/// <summary>
/// Resuelve destinos lógicos a nombres de cola físicos.
/// Hoy: mismo nombre. Permite cambio centralizado en el futuro.
/// </summary>
public class DefaultDestinationResolver : IDestinationResolver
{
    public string Resolve(string logicalDestination) => logicalDestination;
}
