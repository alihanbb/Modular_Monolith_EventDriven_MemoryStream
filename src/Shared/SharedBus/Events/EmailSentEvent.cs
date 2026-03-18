using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Events;

public sealed record EmailSentEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public string EmailType { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
