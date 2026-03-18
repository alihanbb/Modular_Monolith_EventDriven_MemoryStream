using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

public sealed class EventHandlerWrapper<T>(IEventHandler<T> inner) : IEventHandlerWrapper
    where T : IEvent
{
    public Type EventType => typeof(T);

    public Task HandleAsync(IEvent @event, CancellationToken cancellationToken)
        => inner.HandleAsync((T)@event, cancellationToken);
}
