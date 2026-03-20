namespace ModularMonolith.Shared.EventBus;

public class InboxMessage
{
    /// <summary>Event.Id ile aynı değer — deduplication anahtarı.</summary>
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    /// <summary>W3C traceparent — kaynak request'e izlenebilirlik zinciri.</summary>
    public string? TraceContext { get; set; }
}
