namespace ModularMonolith.Shared.EventBus;
public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
