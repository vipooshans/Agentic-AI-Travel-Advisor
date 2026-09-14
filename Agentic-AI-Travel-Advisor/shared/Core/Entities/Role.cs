namespace TravelAdvisor.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Users { get; set; } = [];
}
