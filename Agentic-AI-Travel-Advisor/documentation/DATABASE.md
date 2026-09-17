# Database Documentation

## Setup

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

Connection string in `web-api/appsettings.json`.

## Tables

| Table | Description |
|-------|-------------|
| Roles | RBAC roles |
| AspNetUsers | Users with profile + RoleId |
| Destinations | Travel destinations |
| Hotels | Hotel listings (owner FK) |
| Rooms | Hotel rooms |
| TravelPackages | Agent packages (destination FK) |
| PackageActivities | Activities within packages (Day 2) |
| Bookings | Room or package reservations |
| TravelPreferences | User preferences (1:1) |
| Itineraries / ItineraryItems | Trip plans |
| AIConversations | AI chat history |

## Seed Data

On API startup, `DbSeeder` creates:
- 4 roles + demo admin/owner/agent accounts
- 5 destinations (Paris, Tokyo, Bali, New York, Rome)
- 2 hotels with 4 rooms (owner account)
- 3 travel packages with 7 activities (agent account)

## Migrations

- `InitialCreate` — core schema
- `AddPackageActivity` — PackageActivities table
