using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.AI;
using TravelAdvisor.Infrastructure.AI.Agents;
using TravelAdvisor.Infrastructure.AI.Safety;
using TravelAdvisor.Infrastructure.AI.Tools;

namespace TravelAdvisor.UnitTests.AI;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Replays scripted LLM rounds and records what the orchestrator sent.</summary>
internal sealed class ScriptedLlm : ILlmClient
{
    private readonly Queue<Func<LlmToolResponse>> _rounds = new();

    public bool IsConfigured { get; set; } = true;
    public List<IReadOnlyList<LlmMessage>> Requests { get; } = [];
    public List<IReadOnlyList<LlmToolDefinition>> OfferedTools { get; } = [];

    public ScriptedLlm Then(string? content, params LlmToolCall[] calls)
    {
        _rounds.Enqueue(() => new LlmToolResponse { Content = content, ToolCalls = calls });
        return this;
    }

    public ScriptedLlm ThenThrow(Exception ex)
    {
        _rounds.Enqueue(() => throw ex);
        return this;
    }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool jsonMode = false, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<LlmToolResponse> ChatWithToolsAsync(IReadOnlyList<LlmMessage> messages, IReadOnlyList<LlmToolDefinition> tools, CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToList());
        OfferedTools.Add(tools);
        return Task.FromResult(_rounds.Count > 0 ? _rounds.Dequeue()() : new LlmToolResponse { Content = "Done." });
    }

    public static LlmToolCall Call(string name, string json) => new() { Id = "call_" + Guid.NewGuid().ToString("N")[..8], Name = name, ArgumentsJson = json };
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));
}

internal sealed class AiHarness
{
    public static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    public static readonly UserContext User = new("user-1", RoleNames.User);

    public Mock<ICatalogTools> Catalog { get; } = new();
    public Mock<IBookingService> Bookings { get; } = new();
    public Mock<IUserService> Users { get; } = new();
    public ScriptedLlm Llm { get; } = new() { IsConfigured = false };
    public FixedClock Clock { get; } = new(Now);
    public HashSet<int> UnavailableRooms { get; } = [];
    public List<CreateBookingRequest> Created { get; } = [];
    public AiOptions Options { get; } = new();
    public ListLogger<AgentOrchestrator> OrchestratorLog { get; } = new();

    public IConfiguration Configuration { get; } = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Key"] = "unit-test-jwt-signing-key-0123456789",
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=t;Username=u;Password=UnitTestDbPw!"
    }).Build();

    public AiHarness()
    {
        var destinations = new List<CatalogDestinationMatch>
        {
            AiTestData.Ella,
            new() { Id = 7, Name = "Kandy", Country = "Sri Lanka" }
        };

        Catalog.Setup(c => c.SearchDestinationsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? q, CancellationToken _) => destinations
                .Where(d => string.IsNullOrWhiteSpace(q) || d.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Select(d => new CatalogDestinationMatch { Id = d.Id, Name = d.Name, Country = d.Country, Description = d.Description })
                .ToList());
        Catalog.Setup(c => c.SearchHotelsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string city, int _, decimal? _, CancellationToken _) =>
                AiTestData.EllaRooms().Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList());
        Catalog.Setup(c => c.SearchPackagesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int? id, string? _, decimal? _, int? _, CancellationToken _) =>
                id is null or 6 ? AiTestData.EllaPackages() : []);
        Catalog.Setup(c => c.SearchActivitiesAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int? id, string? _, string? _, decimal? _, CancellationToken _) =>
                id is null or 6 ? AiTestData.EllaActivities() : []);
        Catalog.Setup(c => c.SearchTransportationAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<TransportMode?>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int? id, string? _, string? from, TransportMode? _, decimal? _, CancellationToken _) =>
                AiTestData.EllaTransport().Where(t => from is null || t.FromLocation.Contains(from, StringComparison.OrdinalIgnoreCase)).ToList());
        Catalog.Setup(c => c.GetRoomAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => AiTestData.EllaRooms().FirstOrDefault(r => r.RoomId == id));
        Catalog.Setup(c => c.GetPackageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => AiTestData.EllaPackages().FirstOrDefault(p => p.Id == id));

        Users.Setup(u => u.GetPreferencesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TravelPreferencesDto());

        Bookings.Setup(b => b.CheckAvailabilityAsync(It.IsAny<UserContext?>(), It.IsAny<AvailabilityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserContext? _, AvailabilityQuery q, CancellationToken _) => Quote(q));
        Bookings.Setup(b => b.CreateAsync(It.IsAny<UserContext>(), It.IsAny<CreateBookingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserContext caller, CreateBookingRequest r, CancellationToken _) =>
            {
                Created.Add(r);
                var quote = Quote(new AvailabilityQuery { RoomId = r.RoomId, TravelPackageId = r.TravelPackageId, CheckIn = r.CheckIn, CheckOut = r.CheckOut, Guests = r.Guests });
                return new BookingDto
                {
                    Id = 40 + Created.Count,
                    UserId = caller.UserId,
                    RoomId = r.RoomId,
                    TravelPackageId = r.TravelPackageId,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut ?? r.CheckIn,
                    Guests = r.Guests,
                    Status = BookingStatus.Pending,
                    TotalPrice = quote.TotalPrice
                };
            });
    }

    private AvailabilityQuoteDto Quote(AvailabilityQuery q)
    {
        if (q.RoomId is { } roomId)
        {
            var room = AiTestData.EllaRooms().Single(r => r.RoomId == roomId);
            var nights = (int)(q.CheckOut!.Value - q.CheckIn).TotalDays;
            return new AvailabilityQuoteDto
            {
                Available = !UnavailableRooms.Contains(roomId),
                Reason = UnavailableRooms.Contains(roomId) ? "Already booked for those dates." : null,
                RoomId = roomId,
                CheckIn = q.CheckIn,
                CheckOut = q.CheckOut.Value,
                Nights = nights,
                Guests = q.Guests,
                TotalPrice = room.PricePerNight * nights
            };
        }

        var package = AiTestData.EllaPackages().Single(p => p.Id == q.TravelPackageId);
        return new AvailabilityQuoteDto
        {
            Available = true,
            TravelPackageId = package.Id,
            CheckIn = q.CheckIn,
            CheckOut = q.CheckIn.AddDays(package.DurationDays - 1),
            Guests = q.Guests,
            TotalPrice = package.PricePerPerson * q.Guests,
            RemainingPlaces = 8
        };
    }

    public ToolRegistry BuildRegistry()
    {
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var tools = new IAgentTool[]
        {
            new SearchDestinationsTool(Catalog.Object),
            new SearchHotelsTool(Catalog.Object),
            new SearchTravelPackagesTool(Catalog.Object),
            new SearchActivitiesTool(Catalog.Object),
            new SearchTransportationTool(Catalog.Object),
            new CheckAvailabilityTool(Bookings.Object),
            new GetTravelPreferencesTool(Users.Object),
            new GenerateItineraryTool(new RecommendationAgent(), new ItineraryAgent(NullLogger<ItineraryAgent>.Instance), options),
            new CreateBookingTool(Bookings.Object, Catalog.Object, Clock, options)
        };
        return new ToolRegistry(tools, NullLogger<ToolRegistry>.Instance);
    }

    public AgentOrchestrator BuildOrchestrator()
    {
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        return new AgentOrchestrator(
            BuildRegistry(),
            Llm,
            new TravelPlanningAgent(options),
            new BookingAgent(),
            new OutputSanitizer(Configuration),
            Clock,
            options,
            OrchestratorLog);
    }

    public AgentToolContext Context(ToolRegistry registry, UserContext? caller = null, BookingProposal? confirmed = null) => new()
    {
        Caller = caller ?? User,
        Registry = registry,
        Today = DateOnly.FromDateTime(Now.UtcDateTime),
        ConfirmedProposal = confirmed
    };

    public static List<ChatMessageDto> History(params string[] userMessages) =>
        userMessages.Select(m => new ChatMessageDto { Role = "user", Content = m }).ToList();

    public static ChatRequest Request(string message, string? confirmId = null) => new() { Message = message, ConfirmBookingId = confirmId };
}
