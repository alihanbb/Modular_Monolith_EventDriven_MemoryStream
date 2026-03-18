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
}

