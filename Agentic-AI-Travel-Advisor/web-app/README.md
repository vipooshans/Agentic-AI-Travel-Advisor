# Travel Advisor MVC Portal

The MVC portal is the server-rendered web interface for hotel owners, travel agents, and administrators. It is one of the clients in the Agentic AI Travel Advisor system; the ASP.NET Core Web API is responsible for application data and business rules.

## What it does

- Provides sign-in and cookie-based portal sessions.
- Shows role-specific pages for admins, hotel owners, and travel agents.
- Calls the Travel Advisor API through a configured `HttpClient`.
- Forwards the signed-in user's API token through the portal's JWT cookie handler.
- Shows an access-denied page when an authenticated user cannot access a restricted portal feature.

Traveler registration and the traveler-facing experience are provided by the React and Flutter clients rather than this portal.

## Project structure

| Path | Contents |
|---|---|
| `Controllers/` | Account, admin, agent, home, and owner MVC controllers |
| `Views/` | Razor pages grouped by account, admin, agent, owner, and shared UI |
| `Models/` | Portal view models |
| `Services/` | API authentication and other portal services |
| `Middleware/` | HTTP handlers used when calling the API |
| `TravelAdvisor.Web.csproj` | ASP.NET Core 8 web project and shared-project references |

## Requirements

- .NET 8 SDK
- A running Travel Advisor API
- API configuration and its database configured as described in the repository's main [README](../README.md)

The API must be reachable at the portal's configured `ApiSettings:BaseUrl`. The default in the portal configuration is `http://localhost:5000`.

## Run locally

From the repository root:

```powershell
dotnet run --project web-app/TravelAdvisor.Web.csproj --urls http://localhost:7000
```

Open <http://localhost:7000>. Start the API first and ensure its URL matches the portal configuration.

To point the portal at a different API URL for the current PowerShell session:

```powershell
$env:ApiSettings__BaseUrl = 'http://localhost:5080'
dotnet run --project web-app/TravelAdvisor.Web.csproj --urls http://localhost:7000
```

Use the sign-in credentials configured for the API's Development environment. Demo accounts, if enabled, are for local development only; do not use demo credentials in production.

## Authentication and access

The portal uses ASP.NET Core cookie authentication. Unauthenticated requests are directed to `/Account/Login`; authenticated requests denied by authorization are directed to `/Account/AccessDenied`. The cookie is configured with a 24-hour expiry and sliding expiration. The API still enforces its own authentication, role policies, and resource-ownership rules.

## Related documentation

- [Main project README](../README.md)
- [API reference](../documentation/API.md)
- [Architecture overview](../documentation/ARCHITECTURE.md)
- [Testing guide](../testing/README.md)
