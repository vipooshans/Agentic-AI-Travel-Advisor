namespace TravelAdvisor.Core.Entities;

/// <summary>Timestamps maintained automatically by the persistence layer on save.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
