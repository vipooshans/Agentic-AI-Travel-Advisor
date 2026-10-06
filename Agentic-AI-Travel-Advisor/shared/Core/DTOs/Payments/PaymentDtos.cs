using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "LKR";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Simulated payment. Card details are checked for shape only and are never stored or logged.
/// The test card 4000 0000 0000 0002 is always declined.
/// </summary>
public class CreatePaymentRequest
{
    public PaymentMethod Method { get; set; }
    public string? CardNumber { get; set; }
}

public class UpdatePaymentStatusRequest
{
    public PaymentStatus Status { get; set; }
}
