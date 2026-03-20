namespace ModularMonolith.Shared.EventBus;

/// <summary>
/// InboxProcessorService tarafından çağrılan gerçek event handler sarmalayıcısı.
/// IEventHandlerWrapper'dan ayrı tutulur; EventBusBackgroundService bu interface'i kullanmaz.
/// </summary>
public interface IInboxHandlerWrapper
{
    Type EventType { get; }
    Task HandleAsync(IEvent @event, CancellationToken cancellationToken);
}
