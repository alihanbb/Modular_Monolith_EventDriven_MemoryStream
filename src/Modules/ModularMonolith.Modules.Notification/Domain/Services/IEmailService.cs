namespace ModularMonolith.Modules.Notification.Domain.Services;

public interface IEmailService
{
    Task SendPaymentConfirmationEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        decimal amount,
        DateTime processedAt);

    Task SendPointsEarnedEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        int pointsEarned,
        int totalPoints,
        int availablePoints,
        DateTime earnedAt);
}
