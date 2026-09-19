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
| Hotels | Hotel listings (owner FK, `ApprovalStatus`) |
| Rooms | Hotel rooms |
| TravelPackages | Agent packages (destination FK, `ApprovalStatus`) |
| PackageActivities | Activities within packages (Day 2) |
| Bookings | Room or package reservations |
| TravelPreferences | User preferences (1:1) |
| Itineraries / ItineraryItems | Trip plans |
| AIConversations | AI chat history |

## Seed Data

On API startup, `DbSeeder` creates:
- 4 roles + demo admin/owner/agent accounts
- 5 international destinations (Paris, Tokyo, Bali, New York, Rome)
- 2 hotels with 4 rooms (owner account)
- 3 travel packages with 7 activities (agent account)
- Additive Sri Lanka catalog (if Ella is missing): Ella, Kandy, Galle destinations; hotels/rooms; 3-day packages with hill-country, cultural, and fort activities priced for LKR budgets around Rs. 50,000

## Migrations

- `InitialCreate` — core schema
- `AddPackageActivity` — PackageActivities table
- `AddItineraryPlanFields` — Itineraries.EstimatedCost, DestinationId, Summary
- `AddApprovalStatus` — `Hotels.ApprovalStatus` and `TravelPackages.ApprovalStatus` (`Pending=0`, `Approved=1`, `Rejected=2`). Existing rows default to **Approved** so the public catalog stays visible.

`ApprovalStatus` on new hotels/packages is **Pending**. Seeded catalog listings are **Approved**. Public `GET /api/hotels` and `GET /api/packages` filter to Approved. Bookings ignore Cancelled rows for overlap checks.
