namespace TravelAdvisor.Core.Entities;

public class PackageActivity : IAuditable
{
    public int Id { get; set; }
    public int TravelPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Interest tag used for AI matching, e.g. nature, culture, adventure, beach, food.</summary>
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public TravelPackage TravelPackage { get; set; } = null!;
}
