using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Common;

/// <summary>The authenticated caller, extracted from JWT claims by the API layer.</summary>
public sealed record UserContext(string UserId, string Role)
{
    public bool IsAdmin => Role == RoleNames.Admin;
    public bool IsUser => Role == RoleNames.User;
    public bool IsHotelOwner => Role == RoleNames.HotelOwner;
    public bool IsTravelAgent => Role == RoleNames.TravelAgent;
}
