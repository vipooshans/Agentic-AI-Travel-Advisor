using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Microsoft.Extensions.Logging;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure.AI.Agents;
using TravelAdvisor.Infrastructure.AI.Schema;

namespace TravelAdvisor.Infrastructure.AI.Tools;

/// <summary>A backend capability the AI may invoke. Tools never touch the DbContext for writes.</summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }

    /// <summary>JSON Schema for the arguments object; arguments are validated against it before execution.</summary>
    string ParametersSchema { get; }

    /// <summary>Roles allowed to run the tool. Empty means any authenticated role.</summary>
    IReadOnlyCollection<string> AllowedRoles { get; }

    Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken);
}

/// <summary>Per-turn state shared by the orchestrator, agents and tools.</summary>
public sealed class AgentToolContext
{
    public required UserContext Caller { get; init; }
    public required ToolRegistry Registry { get; init; }
    public required DateOnly Today { get; init; }

    /// <summary>
    /// Set only by the server when the user explicitly confirmed this proposal in the current turn.
    /// The LLM cannot set it, so createBooking cannot run on model output alone.
    /// </summary>
    public BookingProposal? ConfirmedProposal { get; init; }

    public BookingProposal? NewProposal { get; set; }
    public BookingDto? CreatedBooking { get; set; }
    public string? BookingError { get; set; }
    public PlanOutcome? LastOutcome { get; set; }
    public List<ToolCallSummary> Calls { get; } = [];
    public List<string> Agents { get; } = [];

    public void UseAgent(string name)
    {
        if (!Agents.Contains(name))
            Agents.Add(name);
    }
}

public sealed class ToolResult
{
    public string Name { get; init; } = string.Empty;
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }

    public static ToolResult Fail(string name, string error) => new() { Name = name, Success = false, Error = error };
}

public sealed class ToolRegistry
{
    private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache = new();
    private readonly Dictionary<string, IAgentTool> _tools;
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(IEnumerable<IAgentTool> tools, ILogger<ToolRegistry> logger)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        _logger = logger;
    }

    public IReadOnlyCollection<string> Names => _tools.Keys;

    /// <summary>Tool definitions the given caller is allowed to see (unauthorized tools are not offered to the LLM).</summary>
    public IReadOnlyList<LlmToolDefinition> DefinitionsFor(UserContext caller) =>
        _tools.Values
            .Where(t => IsAllowed(t, caller))
            .Select(t => new LlmToolDefinition { Name = t.Name, Description = t.Description, ParametersSchema = t.ParametersSchema })
            .ToList();

    private static readonly JsonSerializerOptions ArgumentOptions = new(TravelPlanSchema.JsonOptions)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public Task<ToolResult> ExecuteAsync(string name, object arguments, AgentToolContext context, CancellationToken cancellationToken) =>
        ExecuteAsync(name, SerializeArguments(arguments), context, cancellationToken);

    /// <summary>
    /// Optional tool parameters are typed (not nullable) in the schemas, so null members are omitted.
    /// WhenWritingNull does not apply to dictionary values, hence the explicit pass over the top level.
    /// </summary>
    public static string SerializeArguments(object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments, ArgumentOptions);
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Where(p => p.Value is null).Select(p => p.Key).ToList())
                obj.Remove(key);
        }
        return node?.ToJsonString() ?? "{}";
    }

    public async Task<ToolResult> ExecuteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await RunAsync(name, argumentsJson, context, cancellationToken);
        stopwatch.Stop();

        context.Calls.Add(new ToolCallSummary
        {
            Name = name,
            Success = result.Success,
            Error = result.Error,
            DurationMs = stopwatch.ElapsedMilliseconds
        });
        return result;
    }

    /// <summary>Typed convenience wrapper used by the deterministic agents.</summary>
    public async Task<(T? Data, ToolResult Result)> InvokeAsync<T>(string name, object arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(name, arguments, context, cancellationToken);
        return (result.Success && result.Data is T typed ? typed : default, result);
    }

    private async Task<ToolResult> RunAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            _logger.LogWarning("AI requested unknown tool {Tool}", name);
            return ToolResult.Fail(name, $"Unknown tool '{name}'.");
        }

        if (!IsAllowed(tool, context.Caller))
        {
            _logger.LogWarning("AI tool {Tool} denied for role {Role}", name, context.Caller.Role);
            return ToolResult.Fail(name, $"The {name} tool is not available to the {context.Caller.Role} role.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        }
        catch (JsonException)
        {
            _logger.LogWarning("AI tool {Tool} received malformed JSON arguments", name);
            return ToolResult.Fail(name, "Arguments must be a JSON object.");
        }

        using (document)
        {
            var schema = SchemaCache.GetOrAdd(tool.Name, _ => JsonSchema.FromText(tool.ParametersSchema));
            var evaluation = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!evaluation.IsValid)
            {
                var errors = (evaluation.Details ?? [])
                    .Where(d => d.Errors is not null)
                    .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))
                    .Take(5)
                    .ToList();
                _logger.LogWarning("AI tool {Tool} arguments failed schema validation: {Errors}", name, string.Join("; ", errors));
                return ToolResult.Fail(name, "Invalid arguments: " + (errors.Count > 0 ? string.Join("; ", errors) : "schema validation failed."));
            }

            try
            {
                var data = await tool.ExecuteAsync(document.RootElement, context, cancellationToken);
                return new ToolResult { Name = name, Success = true, Data = data };
            }
            catch (AppException ex)
            {
                return ToolResult.Fail(name, ex.Message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI tool {Tool} failed", name);
                return ToolResult.Fail(name, "The tool failed unexpectedly. No changes were made.");
            }
        }
    }

    private static bool IsAllowed(IAgentTool tool, UserContext caller) =>
        tool.AllowedRoles.Count == 0 || tool.AllowedRoles.Contains(caller.Role);
}
