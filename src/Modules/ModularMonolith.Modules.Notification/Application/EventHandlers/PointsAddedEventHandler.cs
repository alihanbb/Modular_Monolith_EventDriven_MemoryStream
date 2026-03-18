using Microsoft.Extensions.Logging;
using ModularMonolith.Modules.Notification.Domain.Services;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;

namespace ModularMonolith.Modules.Notification.Application.EventHandlers;

internal sealed class PointsAddedEventHandler(
    IEmailService emailService,
    IEventBus eventBus,
    ILogger<PointsAddedEventHandler> logger) : IEventHandler<PointsAddedEvent>
{
    public async Task HandleAsync(PointsAddedEvent @event, CancellationToken cancellationToken = default)
    {
        var recipientEmail = @event.UserEmail;
        bool success = false;
        string? errorMessage = null;

        logger.LogInformation(
            " Puan bildirim e-postası gönderiliyor. UserId: {UserId}, Puan: {Points}",
            @event.UserId, @event.PointsAdded);

        try
        {
            await emailService.SendPointsEarnedEmailAsync(
                recipientEmail, @event.UserId, @event.OrderId,
                @event.PointsAdded, @event.TotalPoints, @event.AvailablePoints, @event.OccurredAt);

            success = true;
            logger.LogInformation(" Puan bildirim e-postası gönderildi: {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogError(ex, " Puan bildirim e-postası gönderilemedi: {Email}", recipientEmail);
        }

        await eventBus.PublishAsync(new EmailSentEvent
        {
            UserId = @event.UserId,
            OrderId = @event.OrderId,
            EmailType = "PointsEarned",
            RecipientEmail = recipientEmail,
            Success = success,
            ErrorMessage = errorMessage
        }, cancellationToken);
    }
}
