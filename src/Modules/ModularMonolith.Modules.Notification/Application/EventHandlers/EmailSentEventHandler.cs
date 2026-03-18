using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;

namespace ModularMonolith.Modules.Notification.Application.EventHandlers;

internal sealed class EmailSentEventHandler(
    ILogger<EmailSentEventHandler> logger) : IEventHandler<EmailSentEvent>
{
    public Task HandleAsync(EmailSentEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.Success)
        {
            logger.LogInformation(
                "E-posta başarıyla gönderildi. Tür: {EmailType}, Alıcı: {Email}, " +
                "UserId: {UserId}, OrderId: {OrderId}",
                @event.EmailType, @event.RecipientEmail, @event.UserId, @event.OrderId);
        }
        else
        {
            logger.LogWarning(
                "E-posta gönderilemedi. Tür: {EmailType}, Alıcı: {Email}, " +
                "UserId: {UserId}, OrderId: {OrderId}. Hata: {Error}",
                @event.EmailType, @event.RecipientEmail, @event.UserId, @event.OrderId, @event.ErrorMessage);
        }

        return Task.CompletedTask;
    }
}
