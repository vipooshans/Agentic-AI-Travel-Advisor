using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TravelAdvisor.Infrastructure.AI;
using Xunit;
using Xunit.Abstractions;

namespace TravelAdvisor.UnitTests.AI.Evaluation;

/// <summary>
/// AI evaluation categories A-J, deterministic: no model (deterministic agents) or a scripted model.
/// Set AI_EVAL_REPORT_DIR to write ai-evaluation-report.md/.json with every case's recorded outcome.
/// </summary>
public class AiEvaluationTests(MockedAiEvalReport report, ITestOutputHelper output) : IClassFixture<MockedAiEvalReport>
{
    public static TheoryData<string> CaseIds => new(AiEvalCases.Cases.Select(c => c.Id));

    [Theory]
    [MemberData(nameof(CaseIds))]
    public async Task Ai_evaluation_case(string id)
    {
        var evalCase = AiEvalCases.Get(id);
        var result = await EvalRunner.RunAsync(evalCase);
        report.Record(result);

        output.WriteLine($"{id} {evalCase.Name}");
        output.WriteLine("expected: " + evalCase.Expected);
        output.WriteLine("actual:   " + AiEvalReportFixture.Actual(result));
        Assert.True(result.Passed, $"{id} failed: {string.Join("; ", result.Failures)}");
    }

    [Fact]
    public void Every_category_has_cases_and_ids_are_unique()
    {
        var ids = AiEvalCases.Cases.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        foreach (var category in "ABCDEFGHIJ")
            Assert.True(AiEvalCases.Cases.Count(c => c.Category == category.ToString()) >= 5, $"category {category} has fewer than 5 cases");
    }
}

/// <summary>
/// The live-capable cases against a real OpenAI-compatible model. Skipped unless AI_EVAL_API_KEY is set
/// (optionally AI_EVAL_BASE_URL and AI_EVAL_MODEL). Results go to ai-evaluation-live-report.md/.json.
/// </summary>
public class AiLiveEvaluationTests(LiveAiEvalReport report, ITestOutputHelper output) : IClassFixture<LiveAiEvalReport>
{
    public static TheoryData<string> LiveCaseIds => new(AiEvalCases.Cases.Where(c => c.Live).Select(c => c.Id));

    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("AI_EVAL_API_KEY");

    [SkippableTheory]
    [MemberData(nameof(LiveCaseIds))]
    public async Task Ai_live_evaluation_case(string id)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            report.RecordSkipped(id);
            throw new SkipException("AI_EVAL_API_KEY is not set, so no live model was called.");
        }

        var options = new AiOptions { ApiKey = ApiKey, TimeoutSeconds = 90 };
        if (Environment.GetEnvironmentVariable("AI_EVAL_BASE_URL") is { Length: > 0 } baseUrl)
            options.BaseUrl = baseUrl;
        if (Environment.GetEnvironmentVariable("AI_EVAL_MODEL") is { Length: > 0 } model)
            options.Model = model;

        using var http = new HttpClient();
        var llm = new OpenAiCompatClient(http, Options.Create(options), new LlmAvailability(TimeProvider.System), NullLogger<OpenAiCompatClient>.Instance);
        var evalCase = AiEvalCases.Get(id);
        var result = await EvalRunner.RunAsync(evalCase, llm);
        report.Record(result);

        output.WriteLine($"{id} {evalCase.Name} (model {options.Model})");
        output.WriteLine("actual: " + AiEvalReportFixture.Actual(result));
        Assert.True(result.Passed, $"{id} failed: {string.Join("; ", result.Failures)}");
    }
}
