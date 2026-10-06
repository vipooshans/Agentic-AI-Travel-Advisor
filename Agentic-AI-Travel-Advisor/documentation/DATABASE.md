# Database Documentation

PostgreSQL 16, accessed through EF Core 8 (Npgsql). The EF Core migrations in `shared/Infrastructure/Migrations/` are the source of truth for the schema.

## Setup

The connection string is `ConnectionStrings:DefaultConnection`. It is never committed:
- **Local development:** use user secrets: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=travel_advisor;Username=...;Password=..." --project web-api`.
- **Docker Compose:** it is built from `POSTGRES_*` in `.env`.
- **Elsewhere:** the environment variable `ConnectionStrings__DefaultConnection`.

The API applies pending migrations on startup (`DbSeeder.SeedAsync`). To apply them by hand:

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

`CREATE EXTENSION btree_gist` is part of the `AddDomainModelV2` migration. The database user therefore needs permission to create extensions, or the extension must already exist.

## SQL reference

| File | Contents |
|---|---|
| `database/schemas/full_schema.sql` | **Current** complete schema: the idempotent script of all migrations (`dotnet ef migrations script --idempotent`). On 2026-10-01 it was regenerated and was identical to the committed file. |
| `database/schemas/002_domain_model_v2.sql` | The `AddDomainModelV2` migration on its own, for reviewing what Phase 2 changed |
| `database/schemas/001_initial.sql` | Hand-written sketch of the original schema. **Historical, out of date**; kept for reference only |
| `database/seed/001_roles.sql` | The four roles (the API also creates them on startup) |

To regenerate the reference after adding a migration:

```bash
dotnet ef migrations script --idempotent \
  --project shared/Infrastructure --startup-project web-api \
  -o database/schemas/full_schema.sql
```

## Tables

Every table except the Identity tables, `Roles` and `ItineraryItems` has `CreatedAt` and `UpdatedAt` (UTC). These are set automatically for entities that implement `IAuditable`.

| Table | Purpose | Key columns and rules |
|---|---|---|
| `Roles` | RBAC roles: USER, HOTEL_OWNER, TRAVEL_AGENT, ADMIN | `Name` unique |
| `AspNetUsers` (+ Identity tables) | Accounts | `FirstName`, `LastName`, `RoleId` → Roles, `IsActive` (deactivated users are rejected even with a valid JWT). `NormalizedEmail` is unique. |
| `UserProfiles` | Optional profile, 1:1 with a user | `PhoneNumber`, `Nationality`, `DateOfBirth`, `AvatarUrl`, `Bio`, `PreferredCurrency`; `UserId` unique |
| `TravelPreferences` | Preferences used by the AI, 1:1 with a user | `BudgetMin` ≤ `BudgetMax`, `PreferredClimate`, `Interests`, `AccommodationPreference`, `TransportPreference`; `UserId` unique |
| `Destinations` | Places to visit | (`Name`, `Country`) unique |
| `Hotels` | Hotel listings owned by a HOTEL_OWNER | `OwnerId`, `City`, `Country`, `ApprovalStatus` (0 Pending, 1 Approved, 2 Rejected) |
| `Rooms` | Rooms of a hotel | `PricePerNight` ≥ 0, `Capacity` > 0, `IsAvailable`; (`HotelId`, `Name`) unique |
| `RoomAvailability` | Per-date blocks and price overrides set by the owner | `IsBlocked`, `PriceOverride` ≥ 0; (`RoomId`, `Date`) unique |
| `TravelPackages` | Packages created by a TRAVEL_AGENT | `DestinationId`, `Price` ≥ 0, `DurationDays` > 0, `MaxTravelers` > 0, `ApprovalStatus` |
| `PackageActivities` | Day-by-day activities of a package | `DayNumber` > 0, `Price` ≥ 0, `Category`, `SortOrder` |
| `Transportation` | Transport options offered by an agent | `Mode`, `FromLocation`, `ToLocation`, `DepartureTime`, `DurationMinutes` > 0, `PricePerPerson` ≥ 0, `Capacity` > 0, `IsActive`; optional `TravelPackageId`, `DestinationId` |
| `Bookings` | Room or package reservations | Exactly one of `RoomId` / `TravelPackageId`; `CheckOut` > `CheckIn`; `Guests` > 0; `TotalPrice` ≥ 0 (computed by the server); `Status` (0 Pending, 1 Confirmed, 2 Cancelled, 3 Completed); `CancelledAt`; `Version` (concurrency token, PostgreSQL `xmin`) |
| `Payments` | Payments recorded against a booking | `Amount` > 0, `Currency`, `Method`, `Status`, `PaidAt`; `TransactionReference` unique |
| `Reviews` | One review per completed booking | `Rating` 1–5; exactly one of `HotelId` / `TravelPackageId`; `BookingId` unique; `Status` (moderation) |
| `Itineraries` / `ItineraryItems` | Saved trip plans | `EndDate` ≥ `StartDate`, `Travelers` > 0, `Budget`, `EstimatedCost`, optional `DestinationId` and `ConversationId`; items have `DayNumber`, `SortOrder`, `ItemType`, `EstimatedCost` |
| `AIConversations` | AI chat history | `Messages` (JSON), `Title`, `Version` (concurrency token) |
| `AIRecommendations` | Items the AI recommended in a conversation | `ItemType` and the id of the hotel, room, package, transport or destination; `EstimatedCost`, `Score`, `Reason` |
| `SystemSettings` | Admin-editable settings | `Key` unique. Seeded keys: `Platform.DefaultCurrency` (LKR), `Booking.MaxAdvanceDays` (365), `Booking.GuestCancellationCutoffHours` (24), `Ai.AssistantEnabled` (true), `Platform.MaintenanceMessage` |

### Double-booking protection

Two layers stop the same room being booked twice for overlapping dates:
1. `BookingService` opens a transaction and locks the room (or package) row, so concurrent requests for it queue up. It then checks for overlapping bookings and blocked dates, and returns `409 Conflict` if there are any.
2. The database enforces the same rule even if two requests race past that check:

```sql
ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_Room_NoOverlap"
    EXCLUDE USING gist ("RoomId" WITH =, tstzrange("CheckIn", "CheckOut", '[)') WITH &&)
    WHERE ("Status" <> 2 AND "RoomId" IS NOT NULL);
```

Cancelled bookings (`Status` 2) do not block dates, and check-out day is free for the next check-in (`[)` range). The k6 race test sends 20 simultaneous requests for the same room and dates. Exactly one booking is created and the other 19 get 409 (`testing/execution-results/phase9-nfr/SUMMARY.md`).

## Seed data

On every start the API:
- applies migrations;
- creates the four roles and the default `SystemSettings`;
- creates an admin from `Seed:AdminEmail` / `Seed:AdminPassword`, if both are set.

The demo data is created only when `Seed:DemoUsers` is true. That is the default in Development only; Docker Compose sets it with `SEED_DEMO_USERS`. The demo data is:
- the accounts `admin@traveladvisor.com`, `owner@traveladvisor.com` and `agent@traveladvisor.com` (passwords in the main README; for local use only);
- international destinations, hotels and packages (Paris, Tokyo, Bali, New York, Rome);
- the Sri Lanka catalog (Ella, Kandy, Galle, with hotels, rooms and 3-day packages priced for LKR budgets around Rs. 50,000).

## Migrations

| Migration | Change |
|---|---|
| `InitialCreate` | Core schema |
| `AddPackageActivity` | `PackageActivities` |
| `AddItineraryPlanFields` | `Itineraries.EstimatedCost`, `DestinationId`, `Summary` |
| `AddApprovalStatus` | `Hotels.ApprovalStatus` and `TravelPackages.ApprovalStatus`. Existing rows default to Approved, so the public catalog stays visible. |
| `AddHotelAndPackageImages` | `ImageUrl` on hotels and packages |
| `AddDomainModelV2` | `UserProfiles`, `RoomAvailability`, `Transportation`, `Payments`, `Reviews`, `AIRecommendations`, `SystemSettings`; audit timestamps; the CHECK constraints and unique indexes above; the no-overlap exclusion constraint; `Itineraries.Budget`, `Travelers`, `ConversationId`; booking `CancelledAt` and concurrency tokens. Existing data is migrated in place (DEF-017). |

The database tests (`tests/TravelAdvisor.Api.Tests`, Testcontainers PostgreSQL) apply all migrations to an empty database. They also cover foreign keys, unique and CHECK constraints, transactions and the exclusion constraint.
