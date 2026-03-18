using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Shared.Events;

public sealed record PointsAddedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public int PointsAdded { get; init; }
    public int TotalPoints { get; init; }
    public int AvailablePoints { get; init; }
}
