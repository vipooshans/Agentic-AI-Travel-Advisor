namespace TravelAdvisor.Core.Common;

/// <summary>
/// Base type for expected business failures. The API exception handler converts these into
/// ProblemDetails responses with the matching HTTP status code.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }
}

public sealed class NotFoundException(string message) : AppException(404, message);

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : AppException(403, message);

public sealed class ConflictException(string message) : AppException(409, message);

public sealed class BusinessRuleException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(400, message, errors);

public sealed class UnauthorizedAppException(string message) : AppException(401, message);
