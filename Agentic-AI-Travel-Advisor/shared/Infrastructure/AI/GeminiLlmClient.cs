using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI;

/// <summary>Google Gemini client using the stable gemini-2.5-flash model via OpenAI-compatible endpoint.</summary>
public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiLlmClient> _logger;

    public GeminiLlmClient(HttpClient http, IOptions<AiOptions> options, ILogger<GeminiLlmClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 300));
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string ProviderName => "Gemini";
    public string BaseUrl => !string.IsNullOrWhiteSpace(_options.BaseUrl) ? _options.BaseUrl.TrimEnd('/') : "https://generativelanguage.googleapis.com/v1beta/openai";
    public string Model => !string.IsNullOrWhiteSpace(_options.Model) ? _options.Model.Trim() : "gemini-2.5-flash";

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject
        {
            ["model"] = Model,
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
            ["model"] = Model,
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
            _logger.LogError("Gemini returned unexpected payload without choices: {Json}", doc.RootElement.GetRawText());
            throw new InvalidOperationException($"Gemini returned unexpected payload: {doc.RootElement.GetRawText()}");
        }

        var message0 = choices[0].GetProperty("message");
        var textContent = message0.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

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

        return new LlmToolResponse { Content = textContent, ToolCalls = toolCalls };
    }

    private async Task<JsonDocument> SendAsync(JsonObject payload, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured.");

        var key = _options.ApiKey.Trim();
        var url = $"{BaseUrl}/chat/completions";

        var modelsToTry = new List<string> { Model, "gemini-2.5-flash", "gemini-2.5-flash-lite" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string lastErrorBody = string.Empty;
        HttpStatusCode lastStatusCode = HttpStatusCode.InternalServerError;

        foreach (var modelName in modelsToTry)
        {
            payload["model"] = modelName;

            var delays = new[] { 1000, 2000, 4000 };

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                request.Headers.TryAddWithoutValidation("x-goog-api-key", key);
                request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling Gemini (Model: {Model}, BaseUrl: {BaseUrl}, Attempt: {Attempt})", modelName, BaseUrl, attempt);

                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return JsonDocument.Parse(body);
                }

                lastStatusCode = response.StatusCode;
                lastErrorBody = body;

                _logger.LogWarning("Gemini request for model {Model} returned {Status}: {Body}",
                    modelName, (int)response.StatusCode, body.Length > 200 ? body[..200] : body);

                // DO NOT retry 400, 401, 403, or other client errors.
                // ONLY retry on 503 (Service Unavailable) or 429 (Too Many Requests).
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
                {
                    if (attempt <= 3)
                    {
                        var delayMs = delays[attempt - 1];
                        _logger.LogInformation("Transient {Status} from Gemini. Backing off for {DelayMs}ms before attempt {NextAttempt}...",
                            (int)response.StatusCode, delayMs, attempt + 1);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }
                }

                break;
            }
        }

        _logger.LogError("Gemini call failed. Status: {Status}, Body: {Body}", (int)lastStatusCode, lastErrorBody);
        throw new HttpRequestException($"Gemini request failed ({(int)lastStatusCode}): {lastErrorBody}", null, lastStatusCode);
    }
}
