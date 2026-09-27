using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

public class Hotel
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    public ApplicationUser Owner { get; set; } = null!;
    public ICollection<Room> Rooms { get; set; } = [];
}
