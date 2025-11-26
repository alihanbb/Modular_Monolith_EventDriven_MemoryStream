using MassTransit;
using NotificationService.Email.Services;
using Shared.Event;

namespace NotificationService.Consumer;

public class PaymentSuccessConsumer : IConsumer<PaymentSucceedEvent>
{
    private readonly ILogger<PaymentSuccessConsumer> _logger;
    private readonly IEmailService _emailService;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentSuccessConsumer(
        ILogger<PaymentSuccessConsumer> logger,
        IEmailService emailService,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _emailService = emailService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentSucceedEvent> context)
    {
        var message = context.Message;
        var recipientEmail = $"{message.UserId}@example.com";
        
        bool emailSuccess = false;
        string? errorMessage = null;

        _logger.LogInformation(
            "📧 [EMAIL] Kullanıcı {UserId}, Sipariş {OrderId} için ödeme onay maili işleniyor",
            message.UserId, message.OrderId);

        try
        {
            await _emailService.SendPaymentConfirmationEmailAsync(
                recipientEmail,
                message.UserId,
                message.OrderId,
                message.Amount,
                message.CreatedAt);

            emailSuccess = true;
            
            _logger.LogInformation(
                "✅ [EMAIL] Ödeme onay maili başarıyla gönderildi: {Email}",
                recipientEmail);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex,
                "❌ [EMAIL] Email gönderimi başarısız: {Email}",
                recipientEmail);
        }
        try
        {
            var emailSentEvent = new EmailSentEvent
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                OrderId = message.OrderId,
                EmailType = "PaymentConfirmation",
                RecipientEmail = recipientEmail,
                Success = emailSuccess,
                ErrorMessage = errorMessage,
                SentAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(emailSentEvent);

            _logger.LogInformation(
                "📢 [EMAIL] EmailSentEvent yayınlandı - Kullanıcı: {UserId}, Başarılı: {Success}",
                message.UserId, emailSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [EMAIL] EmailSentEvent yayınlanamadı - Kullanıcı: {UserId}",
                message.UserId);
        }
    }
}
