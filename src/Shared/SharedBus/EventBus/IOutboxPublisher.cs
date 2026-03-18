namespace ModularMonolith.Shared.EventBus;

public interface IOutboxPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
}
