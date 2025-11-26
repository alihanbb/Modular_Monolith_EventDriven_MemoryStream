using MassTransit;
using NotificationService.Email.Services;
using Shared.Event;

namespace NotificationService.Consumer;

public class PointsNotificationConsumer : IConsumer<PointsAddedEvent>
{
    private readonly ILogger<PointsNotificationConsumer> _logger;
    private readonly IEmailService _emailService;
    private readonly IPublishEndpoint _publishEndpoint;

    public PointsNotificationConsumer(
        ILogger<PointsNotificationConsumer> logger,
        IEmailService emailService,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _emailService = emailService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PointsAddedEvent> context)
    {
        var message = context.Message;
        var recipientEmail = $"{message.UserId}@example.com";
        
        bool emailSuccess = false;
        string? errorMessage = null;

        _logger.LogInformation(
            "🎁 [PUAN-BİLDİRİM] Kullanıcı {UserId} için puan ekleme bildirimi işleniyor, Kazanılan: {Points} puan",
            message.UserId, message.PointsAdded);

        try
        {
            await _emailService.SendPointsEarnedEmailAsync(
                recipientEmail,
                message.UserId,
                message.OrderId,
                message.PointsAdded,
                message.TotalPoints,
                message.AvailablePoints,
                message.CreatedAt);

            emailSuccess = true;
            
            _logger.LogInformation(
                "✅ [PUAN-BİLDİRİM] Puan kazanma bildirimi başarıyla gönderildi: {Email}",
                recipientEmail);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex,
                "❌ [PUAN-BİLDİRİM] Puan bildirimi email gönderimi başarısız: {Email}",
                recipientEmail);
        }

        // Publish EmailSentEvent
        try
        {
            var emailSentEvent = new EmailSentEvent
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                OrderId = message.OrderId,
                EmailType = "PointsEarned",
                RecipientEmail = recipientEmail,
                Success = emailSuccess,
                ErrorMessage = errorMessage,
                SentAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(emailSentEvent);

            _logger.LogInformation(
                "📢 [PUAN-BİLDİRİM] EmailSentEvent yayınlandı - Kullanıcı: {UserId}, Başarılı: {Success}",
                message.UserId, emailSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [PUAN-BİLDİRİM] EmailSentEvent yayınlanamadı - Kullanıcı: {UserId}",
                message.UserId);
        }
    }
}
