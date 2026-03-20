using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

/// <summary>
/// IInboxHandlerWrapper olarak kayıtlıdır; yalnızca InboxProcessorService tarafından çağrılır.
/// Gerçek IEventHandler'ı wrap ederek EventBusBackgroundService döngüsünden izole eder.
/// </summary>
public sealed class InboxHandlerWrapper<TEvent>(IEventHandler<TEvent> handler)
    : IInboxHandlerWrapper
    where TEvent : class, IEvent
{
    public Type EventType => typeof(TEvent);

    public Task HandleAsync(IEvent @event, CancellationToken cancellationToken)
        => handler.HandleAsync((TEvent)@event, cancellationToken);
}
