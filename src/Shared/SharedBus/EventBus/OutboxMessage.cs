namespace ModularMonolith.Shared.EventBus;


public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    /// <summary>W3C traceparent — outbox'tan gelen event'i kaynak request'e bağlar.</summary>
    public string? TraceContext { get; set; }
}
