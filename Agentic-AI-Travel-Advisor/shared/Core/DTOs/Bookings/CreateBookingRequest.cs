using System.ComponentModel.DataAnnotations;

namespace TravelAdvisor.Core.DTOs.Bookings;

public class CreateBookingRequest : IValidatableObject
{
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    [Required]
    public DateTime CheckOut { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoomId.HasValue == TravelPackageId.HasValue)
        {
            yield return new ValidationResult(
                "Choose either a room or a travel package.",
                new[] { nameof(RoomId), nameof(TravelPackageId) });
        }

        if (CheckOut.Date <= CheckIn.Date)
        {
            yield return new ValidationResult(
                "Check-out date must be after check-in date.",
                new[] { nameof(CheckOut) });
        }
    }
}
