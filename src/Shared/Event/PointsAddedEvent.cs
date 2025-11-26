namespace Shared.Event;

public sealed record PointsAddedEvent
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public int PointsAdded { get; init; }
    public int TotalPoints { get; init; }
    public int AvailablePoints { get; init; }
    public DateTime CreatedAt { get; init; }
}
