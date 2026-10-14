using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

/// <summary>Simulated payment record. No real gateway is called.</summary>
public class Payment : IAuditable
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "LKR";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
}
