namespace TravelAdvisor.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 60;
    public double Temperature { get; set; } = 0.2;

    /// <summary>Upper bound on LLM ↔ tool round trips per chat turn.</summary>
    public int MaxToolIterations { get; set; } = 6;

    /// <summary>How long a booking proposal can be confirmed before the user must ask again.</summary>
    public int BookingProposalMinutes { get; set; } = 15;

    /// <summary>Indicative LKR value of one unit of each currency, used only to convert stated budgets.</summary>
    public Dictionary<string, decimal> LkrPerUnit { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 300m,
        ["EUR"] = 325m,
        ["GBP"] = 380m
    };
}
