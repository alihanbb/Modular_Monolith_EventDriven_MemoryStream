using Microsoft.Extensions.Logging;
using ModularMonolith.Modules.Notification.Domain.Services;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;

namespace ModularMonolith.Modules.Notification.Application.EventHandlers;

internal sealed class PaymentSucceededEventHandler(
    IEmailService emailService,
    IEventBus eventBus,
    ILogger<PaymentSucceededEventHandler> logger) : IEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(PaymentSucceededEvent @event, CancellationToken cancellationToken = default)
    {
        var recipientEmail = @event.UserEmail;
        bool success = false;
        string? errorMessage = null;

        logger.LogInformation(
            " Ödeme onay e-postası gönderiliyor. UserId: {UserId}, OrderId: {OrderId}",
            @event.UserId, @event.OrderId);

        try
        {
            await emailService.SendPaymentConfirmationEmailAsync(
                recipientEmail, @event.UserId, @event.OrderId, @event.Amount, @event.OccurredAt);

            success = true;
            logger.LogInformation(" Ödeme onay e-postası gönderildi: {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogError(ex, " E-posta gönderilemedi: {Email}", recipientEmail);
        }

        await eventBus.PublishAsync(new EmailSentEvent
        {
            UserId = @event.UserId,
            OrderId = @event.OrderId,
            EmailType = "PaymentConfirmation",
            RecipientEmail = recipientEmail,
            Success = success,
            ErrorMessage = errorMessage
        }, cancellationToken);
    }
}
