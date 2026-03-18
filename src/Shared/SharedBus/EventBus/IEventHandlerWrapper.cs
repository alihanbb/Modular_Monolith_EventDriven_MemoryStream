namespace ModularMonolith.Shared.EventBus;

public interface IEventHandlerWrapper
{
    Type EventType { get; }

    Task HandleAsync(IEvent @event, CancellationToken cancellationToken);
}
