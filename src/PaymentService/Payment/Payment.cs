using Shared.Event;

namespace PaymentService.Payment;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "credit card";
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}

