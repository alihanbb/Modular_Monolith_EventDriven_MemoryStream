using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Infrastructure.Telemetry;

namespace ModularMonolith.Shared.Infrastructure.EventBus;

public sealed class InboxProcessorService<TDbContext>(
        IServiceProvider serviceProvider,
        ILogger<InboxProcessorService<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    private const int MaxRetries = 3;
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InboxProcessor<{Db}> başlatıldı.", typeof(TDbContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await CheckDeadLettersAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "InboxProcessor<{Db}>: beklenmeyen hata.", typeof(TDbContext).Name);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("InboxProcessor<{Db}> durduruldu.", typeof(TDbContext).Name);
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var messages = await dbContext.Set<InboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.ReceivedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        logger.LogDebug("InboxProcessor<{Db}>: {Count} mesaj işleniyor.", typeof(TDbContext).Name, messages.Count);

        var wrappers = scope.ServiceProvider.GetServices<IInboxHandlerWrapper>().ToList();

        foreach (var message in messages)
        {
            await TryDispatchAsync(message, wrappers, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task CheckDeadLettersAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var deadLetters = await dbContext.Set<InboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount >= MaxRetries)
            .ToListAsync(ct);

        if (deadLetters.Count == 0) return;

        logger.LogCritical(
            "InboxProcessor<{Db}>: {Count} adet dead-letter inbox mesajı tespit edildi!",
            typeof(TDbContext).Name, deadLetters.Count);

        foreach (var dl in deadLetters)
        {
            logger.LogCritical(
                "INBOX DEAD LETTER — Id: {Id}, EventType: {EventType}, RetryCount: {Retry}, Son Hata: {Error}",
                dl.Id, dl.EventType, dl.RetryCount, dl.Error);
        }
    }

    private async Task TryDispatchAsync(InboxMessage message, IEnumerable<IInboxHandlerWrapper> wrappers, CancellationToken ct)
    {
        ActivityContext parentContext = default;
        if (message.TraceContext is not null)
            ActivityContext.TryParse(message.TraceContext, null, isRemote: true, out parentContext);

        var eventShortName = message.EventType.Split('.').Last().Split(',').First();

        using var activity = Tracing.ActivitySource.StartActivity(
            $"inbox.dispatch {eventShortName}",
            ActivityKind.Consumer,
            parentContext);

        activity?.SetTag("messaging.system", "inbox");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.destination", eventShortName);
        activity?.SetTag("inbox.message_id", message.Id.ToString());
        activity?.SetTag("inbox.db_context", typeof(TDbContext).Name);
        activity?.SetTag("inbox.retry_count", message.RetryCount);

        try
        {
            var eventType = Type.GetType(message.EventType)
                ?? throw new InvalidOperationException($"Event tipi bulunamadı: {message.EventType}");

            var @event = JsonSerializer.Deserialize(message.Payload, eventType) as IEvent
                ?? throw new InvalidOperationException($"Deserialize başarısız: {message.EventType}");

            var matchingWrappers = wrappers.Where(w => w.EventType == eventType).ToList();

            if (matchingWrappers.Count == 0)
            {
                logger.LogWarning(
                    "InboxProcessor<{Db}>: '{EventType}' için kayıtlı inbox handler bulunamadı. Id={Id}",
                    typeof(TDbContext).Name, eventType.Name, message.Id);
                message.ProcessedAt = DateTime.UtcNow;
                return;
            }

            await Task.WhenAll(matchingWrappers.Select(w => w.HandleAsync(@event, ct)));

            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation(
                "InboxProcessor<{Db}>: {Type} işlendi. Id={Id}",
                typeof(TDbContext).Name, eventType.Name, message.Id);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            message.Error = ex.Message;

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.message", ex.Message);

            logger.LogWarning(
                "InboxProcessor<{Db}>: {Type} başarısız (retry={Retry}/{Max}). Id={Id}. Hata: {Err}",
                typeof(TDbContext).Name, message.EventType,
                message.RetryCount, MaxRetries, message.Id, ex.Message);
        }
    }
}
