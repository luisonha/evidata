namespace Evidata.Worker.Outbox.Messaging;

public interface IDestinationResolver
{
    string Resolve(string logicalDestination);
}
