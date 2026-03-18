using ModularMonolith.Modules.Payment.Domain.Enums;

namespace ModularMonolith.Modules.Payment.Domain.Entities;

internal class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "credit card";
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}
