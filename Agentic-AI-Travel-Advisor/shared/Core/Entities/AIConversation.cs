namespace TravelAdvisor.Core.Entities;

public class AIConversation : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>JSON array of chat messages (role, content, structured plan).</summary>
    public string Messages { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Maps to PostgreSQL xmin so concurrent turns cannot silently overwrite each other.</summary>
    public uint Version { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<AIRecommendation> Recommendations { get; set; } = [];
}
