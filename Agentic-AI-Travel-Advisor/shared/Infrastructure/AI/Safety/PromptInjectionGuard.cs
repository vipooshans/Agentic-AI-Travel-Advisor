using System.Text;
using System.Text.RegularExpressions;

namespace TravelAdvisor.Infrastructure.AI.Safety;

public static class GuardCategories
{
    public const string InstructionOverride = "instruction_override";
    public const string SystemPrompt = "system_prompt";
    public const string SecretExfiltration = "secret_exfiltration";
    public const string DataExfiltration = "data_exfiltration";
    public const string PrivilegeEscalation = "privilege_escalation";
}

public sealed record GuardResult(bool Blocked, string? Category, string? Refusal)
{
    public static readonly GuardResult Allowed = new(false, null, null);
}

/// <summary>
/// Pattern-based screen for prompt injection, secret/data exfiltration and attempts to make the
/// assistant change bookings, approvals or roles. It runs before any agent or LLM sees the message
/// and is also applied to provider-written catalog text (indirect injection).
/// </summary>
public static class PromptInjectionGuard
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(250);

    private const string SecretTerms =
        @"(passwords?|passwd|pwd|(jwt|client|app|api|signing)[\s_-]?secrets?|secret[\s_-]?(keys?|tokens?|values?)|api[\s_-]?keys?|jwt|signing[\s_-]?keys?|(access|auth|bearer|refresh|session)[\s_-]?tokens?|connection[\s_-]?strings?|credentials?|private[\s_-]?keys?|env(ironment)?[\s_-]?variables?|appsettings|\.env\b)";

    private static readonly (string Category, Regex Pattern)[] Rules =
    [
        (GuardCategories.InstructionOverride, new Regex(
            @"\b(ignore|disregard|forget|override|bypass|skip|drop)\b[^.\n]{0,40}\b(previous|prior|above|earlier|all|any|your|the|these|those|system|safety|security)\b[^.\n]{0,25}\b(instructions?|rules?|prompts?|guidelines?|directions?|polic(y|ies)|guardrails?|restrictions?|constraints?)\b",
            Options, Timeout)),
        (GuardCategories.InstructionOverride, new Regex(
            @"\b(you are now|from now on,? you are|act as|pretend (to be|you are)|role-?play as|switch to)\b[^.\n]{0,30}\b(admin(istrator)?|developer|system|root|superuser|dan|unrestricted|unfiltered|jailbroken)\b",
            Options, Timeout)),
        (GuardCategories.InstructionOverride, new Regex(@"\b(developer|god|dan|debug|sudo)\s+mode\b|\bjailbreak", Options, Timeout)),
        (GuardCategories.InstructionOverride, new Regex(@"<\s*/?\s*(system|assistant|im_start|im_end)\s*>|\[\s*/?\s*(system|inst)\s*\]", Options, Timeout)),

        (GuardCategories.SystemPrompt, new Regex(
            @"\b(show|reveal|print|repeat|display|output|tell|give|share|leak|dump|what\s+(is|are|was|were))\b[^.\n]{0,40}\b(system|initial|hidden|internal|original|developer|secret)\s+(prompt|instructions?|message|rules|configuration|config)\b",
            Options, Timeout)),
        (GuardCategories.SystemPrompt, new Regex(
            @"\b(your|the)\s+(system\s+)?(prompt|instructions you (were|have been) given)\b[^.\n]{0,30}\b(verbatim|word for word|exactly|in full)\b",
            Options, Timeout)),

        (GuardCategories.SecretExfiltration, new Regex(
            @"\b(show|reveal|print|give|tell|share|list|dump|send|expose|leak|display|output|read|fetch|get|what\s*(is|are|'s))\b[^.\n]{0,40}\b" + SecretTerms,
            Options, Timeout)),
        (GuardCategories.SecretExfiltration, new Regex(
            @"\b(database|db|postgres(ql)?|server|admin(istrator)?|smtp|root)\s+(password|credentials?|login|connection|user(name)?)\b",
            Options, Timeout)),

        (GuardCategories.DataExfiltration, new Regex(
            @"\b(select\s+[\w*,\s]+\s+from|drop\s+(table|database)|delete\s+from|insert\s+into|truncate\s+table|alter\s+table|update\s+\w+\s+set|union\s+(all\s+)?select)\b|;\s*--",
            Options, Timeout)),
        (GuardCategories.DataExfiltration, new Regex(
            @"\b(all|other|every|another|someone\s+else'?s?)\s+(users?'?s?|customers?'?s?|guests?'?s?|travell?ers?'?s?|people'?s?|accounts?)\b[^.\n]{0,30}\b(emails?|phones?|numbers?|addresses|bookings?|data|details|passwords?|information|info|names?|profiles?|payments?)\b",
            Options, Timeout)),
        (GuardCategories.DataExfiltration, new Regex(
            @"\b(list|show|dump|export|download|give)\b[^.\n]{0,20}\b(all\s+)?(the\s+)?(users|user\s+table|customers|accounts|database|db\s+tables?)\b",
            Options, Timeout)),

        (GuardCategories.PrivilegeEscalation, new Regex(
            @"\b(mark|set|change|make|force|update|switch)\b[^.\n]{0,40}\b(booking|reservation|status)\b[^.\n]{0,30}\b(confirmed|approved|completed|paid)\b",
            Options, Timeout)),
        (GuardCategories.PrivilegeEscalation, new Regex(
            @"\b(confirm|approve|book)\b[^.\n]{0,40}\b(without|bypass(ing)?|skip(ping)?)\b[^.\n]{0,30}\b(payment|provider|owner|agent|approval|availability|checks?|confirmation)\b",
            Options, Timeout)),
        (GuardCategories.PrivilegeEscalation, new Regex(
            @"\b(make|give|grant|promote|upgrade|elevate)\b[^.\n]{0,20}\b(me|my\s+(account|role|user))\b[^.\n]{0,25}\b(admin(istrator)?|hotel[\s_]owner|travel[\s_]agent|provider|superuser|root)\b",
            Options, Timeout)),
        (GuardCategories.PrivilegeEscalation, new Regex(
            @"\bapprove\s+(my|this|the|our)\s+(hotel|package|listing|review)s?\b",
            Options, Timeout))
    ];

    public static GuardResult Inspect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return GuardResult.Allowed;

        var normalized = Normalize(text);
        foreach (var (category, pattern) in Rules)
        {
            try
            {
                if (pattern.IsMatch(normalized))
                    return new GuardResult(true, category, RefusalFor(category));
            }
            catch (RegexMatchTimeoutException)
            {
                return new GuardResult(true, GuardCategories.InstructionOverride, RefusalFor(GuardCategories.InstructionOverride));
            }
        }

        return GuardResult.Allowed;
    }

    /// <summary>Makes provider-written catalog text safe to hand to an LLM as tool output.</summary>
    public static string? SanitizeUntrusted(string? text, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
        if (Inspect(text).Blocked)
            return "[description removed by safety filter]";
        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..(maxLength - 3)] + "...";
    }

    public static string RefusalFor(string? category) => category switch
    {
        GuardCategories.SecretExfiltration or GuardCategories.SystemPrompt =>
            "I can't share passwords, keys, credentials, internal configuration or my internal instructions. " +
            "I can help you plan a trip, compare hotels and packages, or prepare a booking.",
        GuardCategories.DataExfiltration =>
            "I can only use public catalog data and your own account, so I can't look up other people's information or run database commands. " +
            "Ask me about destinations, hotels, packages or your own trip plans.",
        GuardCategories.PrivilegeEscalation =>
            "I can't change booking statuses, approvals or account roles. Bookings I prepare are created as PENDING after you confirm, " +
            "and only the hotel owner, travel agent or an administrator can confirm them.",
        _ =>
            "I can't ignore my safety rules. I'm happy to help you plan a trip, compare options within your budget, or prepare a booking."
    };

    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text.Normalize(NormalizationForm.FormKC))
        {
            if (ch is '\u200B' or '\u200C' or '\u200D' or '\u2060' or '\uFEFF')
                continue;
            builder.Append(char.IsWhiteSpace(ch) ? ' ' : ch);
        }

        return Regex.Replace(builder.ToString(), @"\s{2,}", " ");
    }
}
