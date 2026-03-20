using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

public static class EventBusServiceExtensions
{
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.AddHostedService<EventBusBackgroundService>();
        return services;
    }

    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddKeyedScoped<IOutboxPublisher, OutboxPublisher<TDbContext>>(typeof(TDbContext).Name);
        services.AddHostedService<OutboxProcessorService<TDbContext>>();
        return services;
    }

    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class, IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IEventHandlerWrapper>(sp =>
            new EventHandlerWrapper<TEvent>(sp.GetRequiredService<THandler>()));
        return services;
    }

    /// <summary>
    /// Inbox pattern ile event handler kaydeder.
    /// EventBusBackgroundService → InboxSaverWrapper (DB'ye yazar)
    /// InboxProcessorService → InboxHandlerWrapper → THandler (gerçek işlem)
    /// </summary>
    public static IServiceCollection AddInboxEventHandler<TEvent, THandler, TDbContext>(
        this IServiceCollection services)
        where TEvent : class, IEvent
        where THandler : class, IEventHandler<TEvent>
        where TDbContext : DbContext
    {
        // EventBusBackgroundService tarafından çağrılır — event'i inbox tablosuna yazar
        services.AddSingleton<IEventHandlerWrapper>(sp =>
            new InboxSaverWrapper<TEvent, TDbContext>(sp, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InboxSaverWrapper<TEvent, TDbContext>>>()));

        // InboxProcessorService tarafından çağrılır — gerçek iş mantığını çalıştırır
        services.AddScoped<THandler>();
        services.AddScoped<IInboxHandlerWrapper>(sp =>
            new InboxHandlerWrapper<TEvent>(sp.GetRequiredService<THandler>()));

        return services;
    }

    /// <summary>
    /// Belirtilen DbContext için InboxProcessorService background service'ini kaydeder.
    /// </summary>
    public static IServiceCollection AddInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddHostedService<InboxProcessorService<TDbContext>>();
        return services;
    }
}

