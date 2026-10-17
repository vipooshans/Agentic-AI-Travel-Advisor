using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI;

/// <summary>Client for any OpenAI-compatible chat-completions endpoint (OpenAI, Groq, Gemini's OpenAI API, ...).</summary>
public class OpenAiCompatClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly LlmAvailability _availability;
    private readonly ILogger<OpenAiCompatClient> _logger;

    public OpenAiCompatClient(HttpClient http, IOptions<AiOptions> options, LlmAvailability availability, ILogger<OpenAiCompatClient> logger)
    {
        _http = http;
        _options = options.Value;
        _availability = availability;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 300));
    }

    /// <summary>True when an API key is present in configuration or environment.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey) && !_availability.IsSuspended;

    private bool IsAnthropic =>
        (!string.IsNullOrWhiteSpace(_options.ApiKey) && _options.ApiKey.Trim().StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase))
        || (!string.IsNullOrWhiteSpace(_options.BaseUrl) && _options.BaseUrl.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase));

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default)
    {
        if (IsAnthropic)
        {
            var res = await ChatWithToolsAnthropicAsync(
                new List<LlmMessage>
                {
                    new() { Role = LlmRoles.System, Content = systemPrompt },
                    new() { Role = LlmRoles.User, Content = userPrompt }
                },
                Array.Empty<LlmToolDefinition>(),
                cancellationToken);
            return res.Content ?? string.Empty;
        }

        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["temperature"] = _options.Temperature,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = userPrompt }
            }
        };

        if (jsonMode)
            payload["response_format"] = new JsonObject { ["type"] = "json_object" };

        using var doc = await SendAsync(payload, cancellationToken);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").TryGetProperty("content", out var content)
            ? content.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    public async Task<LlmToolResponse> ChatWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        if (IsAnthropic)
        {
            return await ChatWithToolsAnthropicAsync(messages, tools, cancellationToken);
        }

        var messageArray = new JsonArray();
        foreach (var message in messages)
        {
            var node = new JsonObject { ["role"] = message.Role, ["content"] = message.Content };
            if (message.ToolCallId is not null)
                node["tool_call_id"] = message.ToolCallId;
            if (message.ToolCalls is { Count: > 0 })
            {
                var calls = new JsonArray();
                foreach (var call in message.ToolCalls)
                {
                    calls.Add(new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson }
                    });
                }

                node["tool_calls"] = calls;
            }

            messageArray.Add(node);
        }

        var toolArray = new JsonArray();
        foreach (var tool in tools)
        {
            toolArray.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonNode.Parse(tool.ParametersSchema)
                }
            });
        }

        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["temperature"] = _options.Temperature,
            ["messages"] = messageArray
        };
        if (toolArray.Count > 0)
        {
            payload["tools"] = toolArray;
            payload["tool_choice"] = "auto";
        }

        using var doc = await SendAsync(payload, cancellationToken);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            _logger.LogError("LLM returned unexpected payload without choices: {Json}", doc.RootElement.GetRawText());
            throw new InvalidOperationException($"LLM returned unexpected payload: {doc.RootElement.GetRawText()}");
        }

        var message0 = choices[0].GetProperty("message");
        var content = message0.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

        var toolCalls = new List<LlmToolCall>();
        if (message0.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in tc.EnumerateArray())
            {
                var function = call.GetProperty("function");
                string argsString = "{}";
                if (function.TryGetProperty("arguments", out var args))
                {
                    argsString = args.ValueKind == JsonValueKind.String ? args.GetString() ?? "{}" : args.GetRawText();
                }

                toolCalls.Add(new LlmToolCall
                {
                    Id = call.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N"),
                    Name = function.GetProperty("name").GetString() ?? string.Empty,
                    ArgumentsJson = argsString
                });
            }
        }

        return new LlmToolResponse { Content = content, ToolCalls = toolCalls };
    }

    private async Task<JsonDocument> SendAsync(JsonObject payload, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("AI API key is not configured.");

        var key = _options.ApiKey.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1"
            : _options.BaseUrl.TrimEnd('/');
        var url = SafeLogUrl($"{baseUrl}/chat/completions");
        var logUrl = url;
        var modelName = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim();
        payload["model"] = modelName;
        var retryDelays = new[] { 1000, 2000, 4000 };
        HttpStatusCode lastStatusCode = HttpStatusCode.InternalServerError;

        for (var attempt = 1; attempt <= retryDelays.Length + 1; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            if (url.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation("x-goog-api-key", key);
            }
            request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending LLM request to {ProviderUrl} with model {Model} (attempt {Attempt})",
                logUrl, modelName, attempt);

            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonDocument.Parse(body);
            }

            lastStatusCode = response.StatusCode;
            _logger.LogWarning("LLM request to {ProviderUrl} for model {Model} returned status code {StatusCode}",
                logUrl, modelName, (int)lastStatusCode);

            if (lastStatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                _availability.Suspend();

            var retryable = lastStatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;
            if (!retryable || attempt > retryDelays.Length)
                break;

            await Task.Delay(retryDelays[attempt - 1], cancellationToken);
        }

        _logger.LogError("LLM request to {ProviderUrl} for model {Model} failed with status code {StatusCode}",
            logUrl, modelName, (int)lastStatusCode);
        throw new HttpRequestException($"LLM request failed with status code {(int)lastStatusCode}.", null, lastStatusCode);
    }

    private static string SafeLogUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var safeUri = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty
        };
        return safeUri.Uri.AbsoluteUri;
    }

    private async Task<LlmToolResponse> ChatWithToolsAnthropicAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("AI API key is not configured.");

        var key = _options.ApiKey.Trim();
        var url = "https://api.anthropic.com/v1/messages";

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

            // Check if previous message in messagesArray has the same role -> merge contents to guarantee strict alternation
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
                // First message in Anthropic MUST be user
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

        // Tools definition for Claude
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

        var modelsToTry = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.Model) && _options.Model.Contains("claude", StringComparison.OrdinalIgnoreCase))
        {
            modelsToTry.Add(_options.Model.Trim());
        }
        modelsToTry.Add("claude-3-5-haiku-20241022");
        modelsToTry.Add("claude-3-5-sonnet-20241022");
        modelsToTry.Add("claude-3-haiku-20240307");
        modelsToTry = modelsToTry.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        string lastErrorBody = string.Empty;
        HttpStatusCode lastStatusCode = HttpStatusCode.InternalServerError;

        foreach (var modelName in modelsToTry)
        {
            for (var attempt = 1; attempt <= 2; attempt++)
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

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("x-api-key", key);
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
                request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending Claude request to {Url} with model {Model} (attempt {Attempt})", url, modelName, attempt);

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

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
                {
                    await Task.Delay(1000 * attempt, cancellationToken);
                    continue;
                }

                break;
            }
        }

        _logger.LogError("All Claude attempts failed. Last status: {Status}, body: {Body}", (int)lastStatusCode, lastErrorBody);
        throw new HttpRequestException($"Claude LLM request failed ({(int)lastStatusCode}): {lastErrorBody}", null, lastStatusCode);
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
