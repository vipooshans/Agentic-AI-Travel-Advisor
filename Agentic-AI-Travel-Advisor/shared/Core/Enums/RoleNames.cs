namespace TravelAdvisor.Core.Enums;

public static class RoleNames
{
    public const string User = "USER";
    public const string HotelOwner = "HOTEL_OWNER";
    public const string TravelAgent = "TRAVEL_AGENT";
    public const string Admin = "ADMIN";

    public static readonly string[] All = [User, HotelOwner, TravelAgent, Admin];
}
