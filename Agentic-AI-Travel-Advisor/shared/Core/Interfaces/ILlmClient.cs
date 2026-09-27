namespace TravelAdvisor.Core.Interfaces;

public interface ILlmClient
{
    bool IsConfigured { get; }

    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default);
}
