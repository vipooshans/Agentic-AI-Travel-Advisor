using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI;

public class OpenAiCompatClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiCompatClient(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("AI API key is not configured.");

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["temperature"] = 0.3,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        if (jsonMode)
            payload["response_format"] = new { type = "json_object" };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"LLM request failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? string.Empty;
    }
}
