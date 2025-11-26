using System;

namespace LoyaltyService.Loyalty;

public class UserPoints
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public int UsedPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
