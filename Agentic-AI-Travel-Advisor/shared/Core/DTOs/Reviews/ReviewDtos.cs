using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Reviews;

public class ReviewDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int? HotelId { get; set; }
    public string? HotelName { get; set; }
    public int? TravelPackageId { get; set; }
    public string? PackageTitle { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; }
    /// <summary>First name and last initial only; reviewer emails are never exposed.</summary>
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateReviewRequest
{
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class UpdateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class UpdateReviewStatusRequest
{
    public ReviewStatus Status { get; set; }
}

public class ReviewQuery
{
    public ReviewStatus? Status { get; set; }
    public int? HotelId { get; set; }
    public int? TravelPackageId { get; set; }
}
