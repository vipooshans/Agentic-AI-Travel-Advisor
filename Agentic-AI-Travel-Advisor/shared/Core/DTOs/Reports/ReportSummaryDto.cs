namespace TravelAdvisor.Core.DTOs.Reports;

public class ReportSummaryDto
{
    public int UserCount { get; set; }
    public int HotelOwnerCount { get; set; }
    public int TravelAgentCount { get; set; }
    public int HotelCount { get; set; }
    public int PackageCount { get; set; }
    public int PendingHotelApprovals { get; set; }
    public int PendingPackageApprovals { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal Revenue { get; set; }
}
