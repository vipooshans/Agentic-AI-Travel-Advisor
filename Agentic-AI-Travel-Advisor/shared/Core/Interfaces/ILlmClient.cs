namespace TravelAdvisor.Core.Interfaces;

public interface ILlmClient
{
    bool IsConfigured { get; }

    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default);

    /// <summary>One chat-completions round with function calling (OpenAI "tools" format).</summary>
    Task<LlmToolResponse> ChatWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public static class LlmRoles
{
    public const string System = "system";
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string Tool = "tool";
}

public sealed class LlmMessage
{
    public string Role { get; init; } = LlmRoles.User;
    public string? Content { get; init; }

    /// <summary>Set on role=tool messages: the id of the call this result answers.</summary>
    public string? ToolCallId { get; init; }

    /// <summary>Set on assistant messages that requested tool calls.</summary>
    public IReadOnlyList<LlmToolCall>? ToolCalls { get; init; }
}

public sealed class LlmToolCall
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ArgumentsJson { get; init; } = "{}";
}

public sealed class LlmToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>JSON Schema (draft 2020-12 subset accepted by OpenAI) for the arguments object.</summary>
    public string ParametersSchema { get; init; } = "{}";
}

public sealed class LlmToolResponse
{
    public string? Content { get; init; }
    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = [];
}
