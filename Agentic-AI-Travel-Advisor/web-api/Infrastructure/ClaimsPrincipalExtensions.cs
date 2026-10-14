using System.Security.Claims;
using TravelAdvisor.Core.Common;

namespace TravelAdvisor.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Caller for endpoints that require authentication.</summary>
    public static UserContext ToUserContext(this ClaimsPrincipal principal) =>
        principal.ToOptionalUserContext() ?? throw new UnauthorizedAppException("Authentication is required.");

    /// <summary>Caller for endpoints that also allow anonymous access; null when not signed in.</summary>
    public static UserContext? ToOptionalUserContext(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return null;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = principal.FindFirstValue(ClaimTypes.Role);
        return userId is null || role is null ? null : new UserContext(userId, role);
    }
}
