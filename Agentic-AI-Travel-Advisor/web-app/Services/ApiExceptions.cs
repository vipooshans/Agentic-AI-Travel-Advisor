namespace TravelAdvisor.Web.Services;

/// <summary>The API rejected the portal's token (expired, revoked or signed with an old key).</summary>
public class ApiUnauthorizedException() : Exception("The API rejected the access token.");

/// <summary>The API refused the request for the signed-in user's role or ownership.</summary>
public class ApiForbiddenException() : Exception("The API refused access to this resource.");
