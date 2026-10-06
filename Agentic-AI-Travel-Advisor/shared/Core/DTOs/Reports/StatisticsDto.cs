using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Reports;

public class StatisticsQuery
{
    /// <summary>Inclusive start (booking creation date). Defaults to 12 months ago.</summary>
    public DateTime? From { get; set; }
    /// <summary>Exclusive end. Defaults to tomorrow.</summary>
    public DateTime? To { get; set; }
    public int Top { get; set; } = 5;
}

/// <summary>Booking, revenue and review figures scoped to the caller: platform-wide for admins, own listings for providers.</summary>
public class StatisticsDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalBookings { get; set; }
    public Dictionary<BookingStatus, int> BookingsByStatus { get; set; } = [];
    /// <summary>Sum of confirmed and completed bookings.</summary>
    public decimal Revenue { get; set; }
    public decimal AverageBookingValue { get; set; }
    /// <summary>Cancelled / total, 0-1.</summary>
    public double CancellationRate { get; set; }
    public int TotalGuests { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<MonthlyStatDto> Monthly { get; set; } = [];
    public List<ListingStatDto> TopListings { get; set; } = [];
}

public class MonthlyStatDto
{
    /// <summary>yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}

public class ListingStatDto
{
    public int Id { get; set; }
    /// <summary>"Hotel" or "Package".</summary>
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
