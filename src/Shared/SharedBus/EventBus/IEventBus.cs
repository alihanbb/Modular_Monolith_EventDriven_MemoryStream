namespace ModularMonolith.Shared.EventBus;
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
}
