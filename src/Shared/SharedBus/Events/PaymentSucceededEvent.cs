using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Events;

public sealed record PaymentSucceededEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid OrderId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
