namespace ModularMonolith.Shared.EventBus;

public interface IEventHandler<T> where T : IEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}
