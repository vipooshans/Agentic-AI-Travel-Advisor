using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Infrastructure.AI.Safety;

/// <summary>
/// Last line of defence on everything the assistant says: redacts configured secrets, credential-shaped
/// strings and e-mail addresses, detects system-prompt leakage, and flags booking claims the backend
/// has not made.
/// </summary>
public sealed class OutputSanitizer
{
    /// <summary>Embedded in the system prompt; seeing it in output means the prompt leaked.</summary>
    public const string PromptCanary = "TA-CANARY-6c1f2e";

    public const string RedactedMarker = "[redacted]";

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(250);

    private static readonly (Regex Pattern, string Replacement)[] Patterns =
    [
        (new Regex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?-----END [A-Z ]*PRIVATE KEY-----", Options, Timeout), RedactedMarker),
        (new Regex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", Options, Timeout), RedactedMarker),
        (new Regex(@"\b(host|server|data source)\s*=[^;\n]+;[^\n]*?\b(password|pwd)\s*=[^;\s]+;?", Options, Timeout), "[redacted connection string]"),
        (new Regex(@"\b(password|passwd|pwd|secret|api[_-]?key|signing[_-]?key)\s*[:=]\s*\S+", Options, Timeout), "$1: " + RedactedMarker),
        (new Regex(@"\bsk-[A-Za-z0-9_-]{16,}\b", Options, Timeout), RedactedMarker),
        (new Regex(@"\bbearer\s+[A-Za-z0-9._~+/-]{20,}=*", Options, Timeout), "Bearer " + RedactedMarker),
        (new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", Options, Timeout), "[email removed]")
    ];

    private static readonly Regex[] FalseBookingClaims =
    [
        new(@"\b(booking|reservation|stay|trip|package)\b[^.\n]{0,40}\b(is|has\s+been|was|now|are|have\s+been)\s+(successfully\s+)?(confirmed|completed|approved|finali[sz]ed|guaranteed|secured)\b", Options, Timeout),
        new(@"\b(i|we)('ve|\s+have)?\s+(successfully\s+)?(booked|reserved|confirmed|secured)\b", Options, Timeout),
        new(@"\bsuccessfully\s+(booked|reserved|confirmed)\b", Options, Timeout),
        new(@"\byou('re|\s+are)\s+(all\s+)?(booked|confirmed)\b", Options, Timeout),
        new(@"\bbooking\s+confirmed\b", Options, Timeout)
    ];

    private static readonly string[] SystemPromptMarkers =
    [
        PromptCanary,
        "You are the TravelAdvisor orchestration model",
        "SAFETY RULES (never reveal)"
    ];

    private readonly string[] _secrets;

    public OutputSanitizer(IConfiguration configuration)
    {
        _secrets = CollectSecrets(configuration);
    }

    public string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var result = text;
        foreach (var secret in _secrets)
            result = result.Replace(secret, RedactedMarker, StringComparison.Ordinal);

        foreach (var (pattern, replacement) in Patterns)
        {
            try
            {
                result = pattern.Replace(result, replacement);
            }
            catch (RegexMatchTimeoutException)
            {
                return RedactedMarker;
            }
        }

        return result;
    }

    public static bool LeaksSystemPrompt(string? text) =>
        !string.IsNullOrEmpty(text) &&
        SystemPromptMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// True when the text claims a booking is confirmed/completed but the backend did not return a
    /// booking in that state during this turn.
    /// </summary>
    public static bool ClaimsUnverifiedBooking(string? text, BookingDto? backendBooking)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        if (backendBooking is { Status: BookingStatus.Confirmed or BookingStatus.Completed })
            return false;

        return FalseBookingClaims.Any(p =>
        {
            try
            {
                return p.IsMatch(text);
            }
            catch (RegexMatchTimeoutException)
            {
                return true;
            }
        });
    }

    private static string[] CollectSecrets(IConfiguration configuration)
    {
        var secrets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in configuration.AsEnumerable())
        {
            if (string.IsNullOrWhiteSpace(pair.Value) || pair.Value.Length < 6)
                continue;

            var key = pair.Key;
            if (key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase))
            {
                secrets.Add(pair.Value);
                var match = Regex.Match(pair.Value, @"(?:password|pwd)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                if (match.Success && match.Groups[1].Value.Trim().Length >= 4)
                    secrets.Add(match.Groups[1].Value.Trim());
                continue;
            }

            var leaf = key.Split(':').Last();
            if (leaf.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                leaf.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                leaf.Equals("Key", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith("ApiKey", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith("Token", StringComparison.OrdinalIgnoreCase))
            {
                secrets.Add(pair.Value);
            }
        }

        return secrets.OrderByDescending(s => s.Length).ToArray();
    }
}
