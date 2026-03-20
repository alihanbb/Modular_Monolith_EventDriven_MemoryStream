using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

/// <summary>
/// IEventHandlerWrapper olarak kayıtlıdır; EventBusBackgroundService tarafından çağrılır.
/// Gelen event'i direkt işlemek yerine modülün kendi DB'sine InboxMessage olarak kaydeder.
/// Aynı EventId ikinci kez gelirse UNIQUE constraint sayesinde dedupe edilir.
/// </summary>
public sealed class InboxSaverWrapper<TEvent, TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<InboxSaverWrapper<TEvent, TDbContext>> logger)
    : IEventHandlerWrapper
    where TEvent : class, IEvent
    where TDbContext : DbContext
{
    public Type EventType => typeof(TEvent);

    public async Task HandleAsync(IEvent @event, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var alreadyExists = await dbContext.Set<InboxMessage>()
            .AnyAsync(m => m.Id == @event.Id, cancellationToken);

        if (alreadyExists)
        {
            logger.LogDebug(
                "Inbox dedupe: {EventType} Id={EventId} zaten mevcut, atlandı.",
                typeof(TEvent).Name, @event.Id);
            return;
        }

        var message = new InboxMessage
        {
            Id = @event.Id,
            EventType = typeof(TEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event),
            ReceivedAt = DateTime.UtcNow,
            TraceContext = Activity.Current?.Id
        };

        dbContext.Set<InboxMessage>().Add(message);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogDebug(
                "Inbox'a kaydedildi: {EventType} Id={EventId}",
                typeof(TEvent).Name, @event.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true
                                        || ex.InnerException?.Message.Contains("unique") == true)
        {
            // Concurrent duplicate — güvenli şekilde atla
            logger.LogDebug(
                "Inbox concurrent dedupe: {EventType} Id={EventId}",
                typeof(TEvent).Name, @event.Id);
        }
    }
}
