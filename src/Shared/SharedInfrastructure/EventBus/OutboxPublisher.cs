using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;
public sealed class OutboxPublisher<TDbContext>(TDbContext dbContext) : IOutboxPublisher
    where TDbContext : DbContext
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(T).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            TraceContext = Activity.Current?.Id
        };

        dbContext.Set<OutboxMessage>().Add(message);

        return Task.CompletedTask;
    }
}
