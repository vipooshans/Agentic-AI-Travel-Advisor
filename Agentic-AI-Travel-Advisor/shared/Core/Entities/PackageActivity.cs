namespace TravelAdvisor.Core.Entities;

public class PackageActivity
{
    public int Id { get; set; }
    public int TravelPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }

    public TravelPackage TravelPackage { get; set; } = null!;
}
