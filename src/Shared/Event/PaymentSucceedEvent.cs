namespace Shared.Event;

public sealed record PaymentSucceedEvent
{
    public Guid OrderId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; }
}
