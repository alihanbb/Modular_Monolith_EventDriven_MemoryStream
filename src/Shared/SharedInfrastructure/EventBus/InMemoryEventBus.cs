using System.Threading.Channels;
using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<IEvent> _channel;

    public InMemoryEventBus()
    {
        _channel = Channel.CreateBounded<IEvent>(new BoundedChannelOptions(capacity: 1000)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    internal ChannelReader<IEvent> Reader => _channel.Reader;
}
