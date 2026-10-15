using TravelAdvisor.Core.DTOs.Bookings;

namespace TravelAdvisor.Web.Models;

public sealed class BookingProposalReviewViewModel : ConfirmBookingAgentRequest
{
    public string ProposalTitle { get; set; } = string.Empty;
    public decimal QuotedTotal { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ErrorMessage { get; set; }
}
