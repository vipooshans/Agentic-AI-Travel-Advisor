using TravelAdvisor.Infrastructure;
using TravelAdvisor.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClients", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowClients");
if (!HttpOnlyUrls() && !app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DbSeeder.SeedAsync(app.Services);

app.Run();

static bool HttpOnlyUrls()
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? string.Empty;
    return urls.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           && !urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
