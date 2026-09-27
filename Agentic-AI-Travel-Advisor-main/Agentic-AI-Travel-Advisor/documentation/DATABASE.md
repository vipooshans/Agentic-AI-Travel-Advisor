# Database Documentation

## Overview

PostgreSQL database for the Travel Advisor platform. Schema is managed by **EF Core migrations** in `shared/Infrastructure/Migrations/`.

## Setup

### 1. Create the database

```sql
CREATE DATABASE travel_advisor;
```

### 2. Configure connection string

Update the password in both config files to match your local PostgreSQL install:

- [`web-api/appsettings.json`](../web-api/appsettings.json)
- [`web-app/appsettings.json`](../web-app/appsettings.json)

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=travel_advisor;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 3. Apply migrations

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

On first API startup, `DbSeeder` also runs migrations and seeds roles plus demo accounts.

## Tables

| Table | Description |
|-------|-------------|
| `Roles` | Application RBAC roles |
| `AspNetUsers` | Users (ASP.NET Identity + profile fields) |
| `Hotels` | Hotel listings |
| `Rooms` | Hotel rooms |
| `Destinations` | Travel destinations |
| `TravelPackages` | Agent-created packages |
| `Bookings` | User reservations |
| `TravelPreferences` | User travel preferences (1:1) |
| `Itineraries` | Trip itineraries |
| `ItineraryItems` | Day-by-day itinerary entries |
| `AIConversations` | AI chat history (JSON messages) |

Plus standard ASP.NET Identity tables (`AspNetRoles`, `AspNetUserClaims`, etc.).

## Seed Data

| Role | Email | Password |
|------|-------|----------|
| ADMIN | admin@traveladvisor.com | Admin@123 |
| HOTEL_OWNER | owner@traveladvisor.com | Owner@123 |
| TRAVEL_AGENT | agent@traveladvisor.com | Agent@123 |

Roles: `USER`, `HOTEL_OWNER`, `TRAVEL_AGENT`, `ADMIN`

## Reference SQL

- Schema reference: [`database/schemas/001_initial.sql`](../database/schemas/001_initial.sql)
- Role seed: [`database/seed/001_roles.sql`](../database/seed/001_roles.sql)
