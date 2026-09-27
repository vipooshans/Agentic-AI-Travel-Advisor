using System.ComponentModel.DataAnnotations;

namespace TravelAdvisor.Web.Models;

public class HotelFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class RoomFormViewModel
{
    public int Id { get; set; }
    public int HotelId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string RoomType { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal PricePerNight { get; set; }

    [Range(1, 20)]
    public int Capacity { get; set; } = 2;

    public bool IsAvailable { get; set; } = true;
}

public class PackageFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int DestinationId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Range(1, 365)]
    public int DurationDays { get; set; } = 5;
}

public class ActivityFormViewModel
{
    public int PackageId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 365)]
    public int DayNumber { get; set; } = 1;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    public int SortOrder { get; set; } = 1;
}
