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
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default)
    {
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
                    var callNode = new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson }
                    };
                    if (!string.IsNullOrWhiteSpace(call.ExtraContentJson))
                    {
                        try { callNode["extra_content"] = JsonNode.Parse(call.ExtraContentJson); } catch { }
                    }
                    calls.Add(callNode);
                }

                node["tool_calls"] = calls;
                if (string.IsNullOrEmpty(message.Content))
                {
                    node["content"] = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(message.ExtraContentJson))
            {
                try { node["extra_content"] = JsonNode.Parse(message.ExtraContentJson); } catch { }
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

                string? toolExtra = call.TryGetProperty("extra_content", out var tec) ? tec.GetRawText() : null;

                toolCalls.Add(new LlmToolCall
                {
                    Id = call.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N"),
                    Name = function.GetProperty("name").GetString() ?? string.Empty,
                    ArgumentsJson = argsString,
                    ExtraContentJson = toolExtra
                });
            }
        }

        string? extraContent = message0.TryGetProperty("extra_content", out var ec) ? ec.GetRawText() : null;
        return new LlmToolResponse { Content = content, ToolCalls = toolCalls, ExtraContentJson = extraContent };
    }

    private async Task<JsonDocument> SendAsync(JsonObject payload, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("AI API key is not configured.");

        var key = _options.ApiKey.Trim();
        var baseUrl = _options.BaseUrl.Trim().TrimEnd('/');
        if (baseUrl.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl += "/openai";
        }
        var url = $"{baseUrl}/chat/completions";

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", key);
            request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending LLM request (attempt {Attempt}/{Max}) to {Url} with model {Model}", attempt, maxAttempts, url, _options.Model);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonDocument.Parse(body);
            }

            var statusCode = (int)response.StatusCode;
            var isTransient = statusCode is 503 or 429 or 502 or 504;

            if (isTransient && attempt < maxAttempts)
            {
                _logger.LogWarning("Gemini request returned {StatusCode}. Retrying in {Delay}ms...", statusCode, attempt * 1200);
                await Task.Delay(attempt * 1200, cancellationToken);
                continue;
            }

            _logger.LogError(
                "Gemini/LLM request to {Url} failed with status {StatusCode}: {ErrorBody}",
                url, statusCode, body);
            throw new HttpRequestException($"LLM request failed ({statusCode}): {body}", null, response.StatusCode);
        }

        throw new InvalidOperationException("Unreachable code in SendAsync");
    }
}
