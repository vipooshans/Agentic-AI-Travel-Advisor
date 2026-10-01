using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelAdvisor.Core.Common;

namespace TravelAdvisor.Api.Infrastructure;

/// <summary>
/// Converts exceptions into RFC 7807 problem responses. Every body carries a human-readable <c>message</c>
/// (used by the web and mobile clients) and, for validation failures, an <c>errors</c> map.
/// Unexpected exceptions never expose internal details.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        var (status, message, errors) = Map(exception);
        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request failed with {Status}: {Message}", status, message);

        await ProblemWriter.WriteAsync(httpContext, status, message, errors);
        return true;
    }

    private static (int Status, string Message, IDictionary<string, string[]>? Errors) Map(Exception exception) => exception switch
    {
        AppException app => (app.StatusCode, app.Message, app.Errors),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            "This record was changed by someone else. Reload it and try again.", null),
        DbUpdateException { InnerException: PostgresException pg } => MapPostgres(pg),
        BadHttpRequestException bad => (bad.StatusCode, "The request could not be read.", null),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.", null)
    };

    private static (int, string, IDictionary<string, string[]>?) MapPostgres(PostgresException pg) => pg.SqlState switch
    {
        PostgresErrorCodes.ExclusionViolation => (StatusCodes.Status409Conflict, "Room is already booked for the selected dates.", null),
        PostgresErrorCodes.UniqueViolation => (StatusCodes.Status409Conflict, "A record with the same unique value already exists.", null),
        PostgresErrorCodes.ForeignKeyViolation => (StatusCodes.Status409Conflict, "The record is linked to other data and cannot be changed this way.", null),
        PostgresErrorCodes.CheckViolation => (StatusCodes.Status400BadRequest, "The request violates a data rule.", null),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.", null)
    };
}

public static class ProblemWriter
{
    public static Task WriteAsync(HttpContext httpContext, int status, string message, IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrase(status),
            Detail = message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["message"] = message;
        if (errors is { Count: > 0 })
            problem.Extensions["errors"] = errors;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        return httpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }

    private static string ReasonPhrase(int status) =>
        Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status) is { Length: > 0 } phrase ? phrase : "Error";
}
