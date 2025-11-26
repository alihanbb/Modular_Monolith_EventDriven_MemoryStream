namespace Shared.Event;

public sealed record EmailSentEvent
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public string EmailType { get; init; } = string.Empty; // PaymentConfirmation, PointsNotification
    public string RecipientEmail { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAt { get; init; }
}
