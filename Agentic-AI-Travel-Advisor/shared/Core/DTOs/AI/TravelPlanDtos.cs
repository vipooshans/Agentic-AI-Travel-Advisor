namespace TravelAdvisor.Core.DTOs.AI;

/// <summary>
/// Structured trip plan returned by the AI. Every price and id comes from backend tool results;
/// the plan is validated against <c>TravelPlanSchema</c> before it leaves the server.
/// </summary>
public class TravelPlan
{
    public string Destination { get; set; } = string.Empty;
    public int? DestinationId { get; set; }
    public string? Country { get; set; }

    /// <summary>Trip length in days.</summary>
    public int Duration { get; set; }
    public int Nights { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Travelers { get; set; }
    public decimal Budget { get; set; }
    public string Currency { get; set; } = "LKR";
    public decimal EstimatedTotal { get; set; }
    public bool WithinBudget { get; set; }
    public PlanCostBreakdown CostBreakdown { get; set; } = new();
    public List<PlanHotel> Hotels { get; set; } = [];
    public List<PlanPackage> TravelPackages { get; set; } = [];
    public List<PlanActivity> Activities { get; set; } = [];
    public List<PlanTransport> Transportation { get; set; } = [];
    public List<PlanDay> Itinerary { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class PlanCostBreakdown
{
    public decimal Accommodation { get; set; }
    public decimal Packages { get; set; }
    public decimal Transportation { get; set; }
}

public class PlanHotel
{
    public int HotelId { get; set; }
    public int RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Nights { get; set; }
    public int Rooms { get; set; } = 1;
    public int Capacity { get; set; }
    public decimal TotalCost { get; set; }
    public double? AverageRating { get; set; }
    public bool Selected { get; set; }

    /// <summary>True only when checkAvailability confirmed the dates for this room.</summary>
    public bool AvailabilityChecked { get; set; }
}

public class PlanPackage
{
    public int PackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal PricePerPerson { get; set; }
    public decimal TotalCost { get; set; }
    public int? RemainingPlaces { get; set; }
    public double? AverageRating { get; set; }
    public bool Selected { get; set; }
    public bool AvailabilityChecked { get; set; }
}

public class PlanActivity
{
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int Day { get; set; }
    public decimal PricePerPerson { get; set; }
    public int? PackageId { get; set; }

    /// <summary>False for ideas that are only available as part of a package the plan does not include.</summary>
    public bool IncludedInCost { get; set; }
}

public class PlanTransport
{
    public int TransportationId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerPerson { get; set; }
    public int Trips { get; set; }
    public decimal TotalCost { get; set; }
    public bool Selected { get; set; }
}

public class PlanDay
{
    public int Day { get; set; }
    public DateOnly Date { get; set; }
    public List<PlanDayItem> Items { get; set; } = [];
}

public static class PlanItemTypes
{
    public const string Transport = "transport";
    public const string CheckIn = "checkin";
    public const string CheckOut = "checkout";
    public const string Activity = "activity";
    public const string Free = "free";
}

public class PlanDayItem
{
    /// <summary>Local time as HH:mm.</summary>
    public string Time { get; set; } = "09:00";
    public string Type { get; set; } = PlanItemTypes.Free;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>A booking the assistant prepared from a live availability quote. It is not a booking.</summary>
public class BookingProposal
{
    public string Id { get; set; } = string.Empty;

    /// <summary>"room" or "package".</summary>
    public string Kind { get; set; } = "room";
    public int? HotelId { get; set; }
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Guests { get; set; }
    public decimal QuotedTotal { get; set; }
    public string Currency { get; set; } = "LKR";
    public DateTime ExpiresAt { get; set; }
}

public class ToolCallSummary
{
    public string Name { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
