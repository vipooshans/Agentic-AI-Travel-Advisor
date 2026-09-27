namespace TravelAdvisor.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
}
