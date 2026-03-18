using Microsoft.Extensions.Logging;
using ModularMonolith.Modules.Notification.Domain.Services;

namespace ModularMonolith.Modules.Notification.Infrastructure.Services;


internal sealed class DevEmailService(ILogger<DevEmailService> logger) : IEmailService
{
    public Task SendPaymentConfirmationEmailAsync(
        string recipientEmail, string userId, Guid orderId, decimal amount, DateTime processedAt)
    {
        logger.LogInformation(
            "[DEV] Ödeme onay e-postası gönderilmedi (dev stub). " +
            "Alıcı: {Email}, UserId: {UserId}, OrderId: {OrderId}, Tutar: {Amount:N2} ₺",
            recipientEmail, userId, orderId, amount);

        return Task.CompletedTask;
    }

    public Task SendPointsEarnedEmailAsync(
        string recipientEmail, string userId, Guid orderId,
        int pointsEarned, int totalPoints, int availablePoints, DateTime earnedAt)
    {
        logger.LogInformation(
            "[DEV] Puan bildirim e-postası gönderilmedi (dev stub). " +
            "Alıcı: {Email}, UserId: {UserId}, Kazanılan: {Points} puan",
            recipientEmail, userId, pointsEarned);

        return Task.CompletedTask;
    }
}
