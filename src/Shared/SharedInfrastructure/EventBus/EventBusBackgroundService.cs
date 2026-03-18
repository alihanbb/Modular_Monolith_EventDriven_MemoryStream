using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Infrastructure.Telemetry;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

public sealed class EventBusBackgroundService(
        InMemoryEventBus eventBus,
        IServiceProvider serviceProvider,
        ILogger<EventBusBackgroundService> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventBus arka plan servisi başlatıldı.");

        await foreach (var @event in eventBus.Reader.ReadAllAsync(stoppingToken))
        {
            await DispatchAsync(@event, stoppingToken);
        }

        logger.LogInformation("EventBus arka plan servisi durduruldu.");
    }

    private async Task DispatchAsync(IEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();

        using var activity = Tracing.ActivitySource.StartActivity(
            $"eventbus.dispatch {eventType.Name}",
            ActivityKind.Internal);

        activity?.SetTag("event.type", eventType.Name);
        activity?.SetTag("event.id", @event.Id.ToString());

        logger.LogDebug("Event dispatch ediliyor: {EventType}, Id: {EventId}", eventType.Name, @event.Id);

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var wrappers = scope.ServiceProvider
                .GetServices<IEventHandlerWrapper>()
                .Where(w => w.EventType == eventType)
                .ToList();

            if (wrappers.Count == 0)
            {
                logger.LogWarning("'{EventType}' için kayıtlı handler bulunamadı.", eventType.Name);
                activity?.SetStatus(ActivityStatusCode.Error, "No handlers registered");
                return;
            }

            activity?.SetTag("event.handler_count", wrappers.Count);

            await Task.WhenAll(wrappers.Select(w => InvokeWrapperAsync(w, @event, cancellationToken)));

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Event başarıyla dispatch edildi: {EventType}", eventType.Name);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Event dispatch sırasında hata: {EventType}, Id: {EventId}", eventType.Name, @event.Id);
        }
    }

    private async Task InvokeWrapperAsync(IEventHandlerWrapper wrapper, IEvent @event, CancellationToken cancellationToken)
    {
        using var activity = Tracing.ActivitySource.StartActivity(
            $"eventhandler.handle {wrapper.EventType.Name}",
            ActivityKind.Internal);

        activity?.SetTag("event.type", wrapper.EventType.Name);
        activity?.SetTag("event.id", @event.Id.ToString());
        activity?.SetTag("handler.type", wrapper.GetType().GenericTypeArguments.FirstOrDefault()?.Name ?? wrapper.GetType().Name);

        try
        {
            await wrapper.HandleAsync(@event, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Handler '{HandlerType}' event işlerken hata: {EventType}",
                wrapper.GetType().Name, @event.GetType().Name);
        }
    }
}

