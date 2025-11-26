namespace NotificationService.Email.Services;

public interface IEmailService
{
    Task SendPaymentConfirmationEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        decimal amount,
        DateTime processedAt);

    Task SendPaymentFailedEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        decimal amount,
        string reason,
        DateTime failedAt);

    Task SendPointsEarnedEmailAsync(
        string recipientEmail,
        string userId,
        Guid orderId,
        int pointsEarned,
        int totalPoints,
        int availablePoints,
        DateTime earnedAt);
}
