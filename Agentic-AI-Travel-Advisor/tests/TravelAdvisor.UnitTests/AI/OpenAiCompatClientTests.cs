using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TravelAdvisor.Infrastructure.AI;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

/// <summary>DEF-026: a rejected API key must not cost a provider round trip on every chat request.</summary>
public class OpenAiCompatClientTests
{
    private const string Completion = """{"choices":[{"message":{"content":"ok"}}]}""";

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Rejected_key_suspends_the_client_for_the_cool_down(HttpStatusCode rejection)
    {
        var clock = new ManualClock();
        var handler = new StubHandler(rejection);
        var llm = Client(handler, new LlmAvailability(clock));

        Assert.True(llm.IsConfigured);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => llm.CompleteAsync("system", "user"));
        Assert.Equal(rejection, error.StatusCode);

        Assert.False(llm.IsConfigured);
        clock.Advance(LlmAvailability.CoolDown - TimeSpan.FromSeconds(1));
        Assert.False(llm.IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => llm.CompleteAsync("system", "user"));
        Assert.Equal(1, handler.Calls);

        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(llm.IsConfigured);
        handler.Status = HttpStatusCode.OK;
        Assert.Equal("ok", await llm.CompleteAsync("system", "user"));
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Transient_or_request_errors_do_not_suspend(HttpStatusCode failure)
    {
        var handler = new StubHandler(failure);
        var llm = Client(handler, new LlmAvailability(new ManualClock()));

        await Assert.ThrowsAsync<HttpRequestException>(() => llm.CompleteAsync("system", "user"));

        Assert.True(llm.IsConfigured);
        await Assert.ThrowsAsync<HttpRequestException>(() => llm.CompleteAsync("system", "user"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Suspension_is_shared_across_client_instances()
    {
        var availability = new LlmAvailability(new ManualClock());
        var first = Client(new StubHandler(HttpStatusCode.Unauthorized), availability);
        var secondHandler = new StubHandler(HttpStatusCode.OK);
        var second = Client(secondHandler, availability);

        await Assert.ThrowsAsync<HttpRequestException>(() => first.CompleteAsync("system", "user"));

        Assert.False(second.IsConfigured);
        Assert.Equal(0, secondHandler.Calls);
    }

    [Fact]
    public void Missing_key_is_not_configured()
    {
        var llm = new OpenAiCompatClient(
            new HttpClient(new StubHandler(HttpStatusCode.OK)), Options.Create(new AiOptions { ApiKey = "" }),
            new LlmAvailability(new ManualClock()), NullLogger<OpenAiCompatClient>.Instance);

        Assert.False(llm.IsConfigured);
    }

    private static OpenAiCompatClient Client(StubHandler handler, LlmAvailability availability) =>
        new(new HttpClient(handler), Options.Create(new AiOptions { ApiKey = "test-key", BaseUrl = "https://llm.test/v1" }),
            availability, NullLogger<OpenAiCompatClient>.Instance);

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = status;
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var body = Status == HttpStatusCode.OK ? Completion : """{"error":{"message":"rejected"}}""";
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body) });
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
