using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI;

/// <summary>Anthropic Claude client (claude-3-5-haiku, claude-3-5-sonnet, etc.) via Messages API.</summary>
public class ClaudeLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<ClaudeLlmClient> _logger;

    public ClaudeLlmClient(HttpClient http, IOptions<AiOptions> options, ILogger<ClaudeLlmClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 300));
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string ProviderName => "Claude";
    public string BaseUrl => !string.IsNullOrWhiteSpace(_options.BaseUrl) ? _options.BaseUrl.TrimEnd('/') : "https://api.anthropic.com/v1/messages";
    public string Model => !string.IsNullOrWhiteSpace(_options.Model) ? _options.Model.Trim() : "claude-3-5-haiku-20241022";

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default)
    {
        var res = await ChatWithToolsAsync(
            new List<LlmMessage>
            {
                new() { Role = LlmRoles.System, Content = systemPrompt },
                new() { Role = LlmRoles.User, Content = userPrompt }
            },
            Array.Empty<LlmToolDefinition>(),
            cancellationToken);

        return res.Content ?? string.Empty;
    }

    public async Task<LlmToolResponse> ChatWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Claude API key is not configured.");

        var key = _options.ApiKey.Trim();
        var url = BaseUrl.EndsWith("/messages") ? BaseUrl : $"{BaseUrl}/messages";

        // Extract system prompt
        var systemMsg = messages.FirstOrDefault(m => m.Role == LlmRoles.System);
        var systemPrompt = systemMsg?.Content ?? string.Empty;

        // Build messages array
        var messagesArray = new JsonArray();

        foreach (var msg in messages.Where(m => m.Role != LlmRoles.System))
        {
            string anthropicRole;
            var contentBlocks = new JsonArray();

            if (msg.Role == LlmRoles.Tool || !string.IsNullOrEmpty(msg.ToolCallId))
            {
                anthropicRole = "user";
                contentBlocks.Add(new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = msg.ToolCallId ?? Guid.NewGuid().ToString("N"),
                    ["content"] = msg.Content ?? string.Empty
                });
            }
            else if (msg.Role == LlmRoles.Assistant)
            {
                anthropicRole = "assistant";
                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    contentBlocks.Add(new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = msg.Content
                    });
                }

                if (msg.ToolCalls is { Count: > 0 })
                {
                    foreach (var call in msg.ToolCalls)
                    {
                        JsonNode? inputNode;
                        try
                        {
                            inputNode = JsonNode.Parse(call.ArgumentsJson) ?? new JsonObject();
                        }
                        catch
                        {
                            inputNode = new JsonObject();
                        }

                        contentBlocks.Add(new JsonObject
                        {
                            ["type"] = "tool_use",
                            ["id"] = call.Id,
                            ["name"] = call.Name,
                            ["input"] = inputNode
                        });
                    }
                }

                if (contentBlocks.Count == 0)
                {
                    contentBlocks.Add(new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = string.Empty
                    });
                }
            }
            else
            {
                anthropicRole = "user";
                contentBlocks.Add(new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = msg.Content ?? string.Empty
                });
            }

            // Guarantee turn alternation by merging same-role adjacent blocks
            if (messagesArray.Count > 0 && messagesArray[^1] is JsonObject lastMsg &&
                lastMsg["role"]?.GetValue<string>() == anthropicRole)
            {
                if (lastMsg["content"] is JsonArray existingBlocks)
                {
                    foreach (var block in contentBlocks)
                    {
                        existingBlocks.Add(block?.DeepClone());
                    }
                }
            }
            else
            {
                // First turn must be user
                if (messagesArray.Count == 0 && anthropicRole != "user")
                {
                    messagesArray.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "text", ["text"] = "Hello" }
                        }
                    });
                }

                messagesArray.Add(new JsonObject
                {
                    ["role"] = anthropicRole,
                    ["content"] = contentBlocks
                });
            }
        }

        var toolsArray = new JsonArray();
        foreach (var tool in tools)
        {
            toolsArray.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = JsonNode.Parse(tool.ParametersSchema)
            });
        }

        var modelsToTry = new List<string> { Model, "claude-3-5-haiku-20241022", "claude-3-5-sonnet-20241022" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string lastErrorBody = string.Empty;
        HttpStatusCode lastStatusCode = HttpStatusCode.InternalServerError;

        foreach (var modelName in modelsToTry)
        {
            var payload = new JsonObject
            {
                ["model"] = modelName,
                ["max_tokens"] = 1024,
                ["messages"] = messagesArray.DeepClone()
            };

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                payload["system"] = systemPrompt;
            }

            if (toolsArray.Count > 0)
            {
                payload["tools"] = toolsArray.DeepClone();
            }

            var delays = new[] { 1000, 2000, 4000 };

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("x-api-key", key);
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
                request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling Claude (Model: {Model}, BaseUrl: {BaseUrl}, Attempt: {Attempt})", modelName, BaseUrl, attempt);

                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ParseAnthropicResponse(body);
                }

                lastStatusCode = response.StatusCode;
                lastErrorBody = body;

                _logger.LogWarning("Claude request for model {Model} returned {Status}: {Body}",
                    modelName, (int)response.StatusCode, body.Length > 200 ? body[..200] : body);

                // DO NOT retry 400, 401, 403, or other client errors.
                // ONLY retry on 503 (Service Unavailable) or 429 (Too Many Requests).
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
                {
                    if (attempt <= 3)
                    {
                        var delayMs = delays[attempt - 1];
                        _logger.LogInformation("Transient {Status} from Claude. Backing off for {DelayMs}ms before attempt {NextAttempt}...",
                            (int)response.StatusCode, delayMs, attempt + 1);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }
                }

                break;
            }
        }

        _logger.LogError("Claude call failed. Status: {Status}, Body: {Body}", (int)lastStatusCode, lastErrorBody);
        throw new HttpRequestException($"Claude request failed ({(int)lastStatusCode}): {lastErrorBody}", null, lastStatusCode);
    }

    private static LlmToolResponse ParseAnthropicResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var textBuilder = new StringBuilder();
        var toolCalls = new List<LlmToolCall>();

        if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArray.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (type == "text" && block.TryGetProperty("text", out var textEl))
                {
                    textBuilder.Append(textEl.GetString());
                }
                else if (type == "tool_use")
                {
                    var id = block.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
                    var name = block.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                    var args = block.TryGetProperty("input", out var inputEl) ? inputEl.GetRawText() : "{}";

                    toolCalls.Add(new LlmToolCall
                    {
                        Id = id,
                        Name = name,
                        ArgumentsJson = args
                    });
                }
            }
        }

        var content = textBuilder.Length > 0 ? textBuilder.ToString().Trim() : null;
        return new LlmToolResponse { Content = content, ToolCalls = toolCalls };
    }
}
