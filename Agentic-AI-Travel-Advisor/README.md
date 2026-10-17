# Agentic AI Travel Advisor & Booking System

Travel advisory and booking platform with an agentic AI assistant:

- **Web API** — ASP.NET Core 8, EF Core, PostgreSQL, JWT authentication and role-based authorization for
  `USER`, `HOTEL_OWNER`, `TRAVEL_AGENT` and `ADMIN`.
- **Agentic AI** — an orchestrator with planning, recommendation, itinerary and booking agents that act only
  through schema-validated backend tools. It uses an OpenAI-compatible model with function calling when
  `Ai:ApiKey` is set, and a deterministic planner otherwise.
- **React web app** (`web-react`) — all four roles.
- **MVC portal** (`web-app`) — travelers, hotel owners, travel agents and admins.
- **Flutter app** (`mobile-app`) — travelers.

Booking workflow: `PENDING → CONFIRMED → COMPLETED`, with `CANCELLED` reachable from Pending and Confirmed.
The AI can propose a booking, but a booking is only created after the traveler confirms it, and it starts as
Pending until the provider confirms it.

## Booking & Reservation Module

### Purpose
The Booking & Reservation module is the central transactional engine of the platform. It handles real-time room and tour package availability, price calculations, reservation lifecycles, cancellation enforcement, and payment reconciliation across all client applications.

### Main Features
- **Hotel Room Bookings**: Reservation by check-in / check-out dates, room capacity validation, and automated nightly rate summation.
- **Tour Package Bookings**: Multi-day guided tour reservations with participant count limits and scheduled start dates.
- **Status Lifecycle Management**: Strict state transitions from initial pending state to provider confirmation or completion.
- **Cancellation Policy Enforcement**: Date-driven cancellation validation checking advance cut-off windows before releasing inventory.
- **Simulated Payment Gateway Integration**: Automated payment transaction creation (`Pending` → `Completed`) linked to reservations.
- **Multi-Role Access Control**:
  - **Travelers**: Create bookings, view personal trip history, and cancel eligible reservations.
  - **Hotel Owners**: View incoming room reservations and update status for their own properties.
  - **Travel Agents**: Monitor package bookings and traveler counts for their tour listings.
  - **Admins**: System-wide oversight, search, filtering, and manual status adjustments.

### Booking Status Lifecycle
```
  ┌─────────┐        Provider Confirms        ┌───────────┐        Trip Concludes        ┌───────────┐
  │ PENDING │ ──────────────────────────────> │ CONFIRMED │ ───────────────────────────> │ COMPLETED │
  └─────────┘                                 └───────────┘                              └───────────┘
       │                                            │
       │ Traveler / Provider Cancels                │ Traveler Cancels (Before Cut-off)
       v                                            v
  ┌───────────────────────────────────────────────────────┐
  │                       CANCELLED                       │
  └───────────────────────────────────────────────────────┘
```
- **`PENDING`**: Reservation created by the traveler; room/package held awaiting provider confirmation or payment.
- **`CONFIRMED`**: Provider accepts the booking or payment succeeds; dates and inventory locked.
- **`COMPLETED`**: Check-out date has passed or tour itinerary is finished.
- **`CANCELLED`**: Cancelled by traveler or provider; inventory released.

### Key Entities
- **`Booking`**: Holds `UserId`, `BookingType` (`Hotel` or `Package`), `HotelId`, `RoomId`, `PackageId`, `CheckInDate`, `CheckOutDate`, `StartDate`, `NumberOfGuests`, `TotalPrice`, `Status`, `CreatedAt`, and `UpdatedAt`.
- **`Payment`**: Holds `BookingId`, `Amount`, `Status` (`Pending`, `Completed`, `Failed`, `Refunded`), `PaymentMethod` (`CreditCard`, `DebitCard`, `BankTransfer`, `Cash`), `TransactionId`, and `PaidAt`.

### API Endpoints
| Method | Route | Auth / Role | Purpose |
|---|---|---|---|
| `POST` | `/api/bookings` | `USER` | Create a new hotel room or tour package booking |
| `GET` | `/api/bookings/my-bookings` | `USER` | List all bookings belonging to the authenticated traveler |
| `GET` | `/api/bookings/{id}` | `USER`, `ADMIN` | Get full details and payment record for a single booking |
| `POST` | `/api/bookings/{id}/cancel` | `USER`, `ADMIN` | Cancel an eligible reservation before the cut-off date |
| `GET` | `/api/bookings/owner` | `HOTEL_OWNER` | List reservations for properties owned by the authenticated owner |
| `GET` | `/api/bookings/agent` | `TRAVEL_AGENT` | List bookings for packages managed by the authenticated agent |
| `GET` | `/api/bookings/admin` | `ADMIN` | Search, filter, and audit all bookings across the platform |
| `PUT` | `/api/bookings/{id}/status` | `HOTEL_OWNER`, `TRAVEL_AGENT`, `ADMIN` | Update booking status (`Confirmed`, `Completed`, `Cancelled`) |

### Frontends Consuming the Booking Module
- **React Web App (`web-react`)**: Traveler booking flows, checkout forms, trip list, hotel owner reservation table, and admin booking manager.
- **MVC Portal (`web-app`)**: Public catalog browsing, customer dashboard, hotel/package booking wizards, traveler reservation list with cancellation actions, and owner/agent/admin management desks.
- **Flutter Mobile App (`mobile-app`)**: Mobile booking flows, interactive date pickers, ticket/voucher display, and self-service cancellation.

## Booking AI Agent

### Overview
The Booking AI Agent enables travelers to inspect, create, and manage reservations via natural language conversation. It wraps existing domain services through schema-validated tool calling, prevents double-booking, and guarantees server-side security by taking the user identity directly from verified JWT claims rather than model-generated parameters.

### Tool Calling Functions
The agent interacts with the backend strictly through the following schema-validated tools:
1. `search_hotel_rooms(hotelId, checkIn, checkOut, guests)`: Checks availability and returns nightly rates and room types.
2. `search_packages(query, destination, maxPrice)`: Searches available tour packages matching traveler preferences.
3. `propose_booking(bookingType, referenceId, startDate, endDate, guests)`: Validates inputs, calculates total cost, and creates an in-memory, time-limited booking proposal requiring confirmation.
4. `confirm_booking(proposalId)`: Finalizes the booking by delegating to `IBookingService.CreateBookingAsync`, generating a `PENDING` booking.
5. `get_my_bookings(statusFilter)`: Retrieves the authenticated user's reservations via `IBookingService.GetUserBookingsAsync`.
6. `cancel_booking(bookingId, reason)`: Validates ownership, checks the cancellation cut-off window, and invokes `IBookingService.CancelBookingAsync`.

### Flow: From User Message to Booking Action
1. **User Message**: Traveler types: *"Book a Deluxe Room at Ella Grand Resort from Nov 10 to 14 for 2 guests"*.
2. **Context & Identity**: API controller extracts `UserId` from the verified JWT token (`User.FindFirstValue(ClaimTypes.NameIdentifier)`).
3. **Availability Tool Call**: The agent invokes `search_hotel_rooms`, verifying dates and room capacity against the database.
4. **Proposal & Confirmation Request**: The agent formulates a proposal (Dates, Total Price, Nights) and explicitly asks: *"Would you like me to confirm this booking for LKR 45,000?"*.
5. **User Confirmation**: The user replies *"Yes, please confirm"*.
6. **Execution**: The agent invokes `confirm_booking`, persisting the reservation with `Pending` status.
7. **Response**: A structured confirmation with booking ID, check-in details, and cancellation policies is returned to the client.

### Deterministic Fallback Behavior
If the LLM provider fails, times out, rate limits, or if `Ai:ApiKey` is not configured:
- The system automatically delegates to the **deterministic rule-based planner**.
- Uses regex and intent recognition to handle common commands (e.g., `"my bookings"`, `"show reservations"`, `"cancel booking 14"`, `"book hotel [id]"`).
- Directly queries backend services without hallucinating or failing ungracefully.

The traveler dashboard booking concierge (`POST /api/booking-agent/prompt`) uses a separate
conversational fallback. It remembers recent destination, dates, and guest-count answers, asks for
one missing detail at a time, and produces an availability-checked proposal. A booking is submitted
only when the traveler reviews and submits that proposal.

### Configuration
| Key / Environment Variable | Default | Purpose |
|---|---|---|
| `Ai:ApiKey` / `AI_API_KEY` | *(empty)* | OpenAI/Gemini/Anthropic API key for Chat Completions |
| `Ai:BaseUrl` / `AI_BASE_URL` | `https://api.openai.com/v1` | LLM endpoint URL |
| `Ai:Model` / `AI_MODEL` | `gpt-4o-mini` | LLM model identifier |
| `Ai:BookingProposalMinutes` | `15` | Expiration window (minutes) for unconfirmed booking proposals |
| `RateLimiting:AiPermitsPerMinute` | `20` | Maximum chat requests allowed per authenticated traveler per minute |

## Documentation

| Document | Contents |
|----------|----------|
| [Architecture](documentation/ARCHITECTURE.md) | Layers, request pipeline, security, AI design, clients, deployment, testing overview |
| [API reference](documentation/API.md) | Every endpoint with access rules, request and response shapes, errors, rate limits |
| [Database](documentation/DATABASE.md) | Schema, migrations, constraints, setup |
| [Demo script](documentation/DEMO.md) | Presentation walkthrough |
| [Testing](testing/README.md) | Test plan, test-case catalogue, defect log, recorded results |

Swagger UI is available at `/swagger` when the API runs in Development (or with `Swagger__Enabled=true`).

## Quick start with Docker

```bash
cp .env.example .env      # then set POSTGRES_PASSWORD and JWT_KEY (32+ characters)
docker compose up --build
```

| Service | URL |
|---------|-----|
| API and Swagger | http://localhost:5000/swagger |
| MVC portal | http://localhost:7000 |
| PostgreSQL (from the host) | `localhost:5433` |

The API container runs in Production and applies migrations on startup. Demo accounts and the demo catalog are
only seeded when `SEED_DEMO_USERS=true`. `.env.example` sets it to true for demos; set it to false (the Compose
default) for anything else. Optional: `AI_API_KEY`, `AI_BASE_URL`, `AI_MODEL`.

### Docker hot reload for the MVC portal

For MVC portal development, start Compose with the development override instead:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build web
```

This runs `dotnet watch` in the web container and mounts the source tree, so changes to MVC code and Razor
views are picked up automatically without rebuilding the web image. Keep the command running while developing;
press `Ctrl+C` to stop it. Use `docker compose up --build` without the override for the regular production-style
container.

## Local development

Requirements: .NET 8 SDK, PostgreSQL 16, Node.js 22.12+ (for the React app), Flutter 3 (for the mobile app),
Docker (for the API tests, k6 and ZAP).

### 1. Secrets

No secrets are stored in the repository. The API refuses to start until these are set:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=travel_advisor;Username=postgres;Password=YOUR_PASSWORD" --project web-api
dotnet user-secrets set "Jwt:Key" "A_RANDOM_STRING_OF_AT_LEAST_32_CHARACTERS" --project web-api
```

Environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Key`) work too.

### 2. Web API

```bash
dotnet run --project web-api --urls http://localhost:5080
```

On startup the API applies the EF Core migrations, then seeds roles, settings and, in Development, the demo
accounts and a demo catalog (including Sri Lankan destinations). Without `--urls` the launch profile uses
`http://localhost:5000`; the React dev proxy and the test tools expect port 5080.

To apply migrations without starting the API:

```bash
dotnet ef database update --project shared/Infrastructure --startup-project web-api
```

### 3. React web app

```bash
cd web-react
npm install
npm run dev        # http://localhost:5173, proxies /api to http://localhost:5080
```

`VITE_API_PROXY_TARGET` changes the proxy target (see `web-react/.env.example`). `npm run build` writes
`web-react/dist/`, and `npm run preview` serves it on port 4173.

### 4. MVC portal

```bash
dotnet run --project web-app     # http://localhost:7000
```

It calls the API at `ApiSettings:BaseUrl` (default `http://localhost:5000`).

### 5. Flutter app

```bash
cd mobile-app
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:5080
```

Without `API_BASE_URL` the app uses `http://10.0.2.2:5000` on the Android emulator and `http://localhost:5000`
elsewhere. The app is for `USER` accounts; other roles are told to use the web app.

Android APK (run `powershell -File ../scripts/setup-android.ps1` once if `android/` is missing):

```bash
flutter build apk --release --dart-define=API_BASE_URL=http://YOUR_LAN_IP:5000
```

The APK is written to `mobile-app/build/app/outputs/flutter-apk/app-release.apk`. Cleartext HTTP is allowed
for classroom demos.

**Windows note:** the `&` in this repository's folder name breaks Gradle, the Flutter tool scripts and `npx`.
Map the project folder to a drive letter first, e.g. `subst T: "<path>\Agentic-AI-Travel-Advisor"`, and run
those tools from `T:\`.

### Optional: LLM mode

```bash
dotnet user-secrets set "Ai:ApiKey" "YOUR_KEY" --project web-api
```

Any OpenAI-compatible Chat Completions endpoint works (`Ai:BaseUrl`, `Ai:Model`, default `gpt-4o-mini`).
Without a key, or if the provider rejects it, the deterministic planner answers using the same catalog
tools; after a 401/403 from the provider, LLM calls pause for 10 minutes.

## Configuration

| Key | Default | Purpose |
|-----|---------|---------|
| `ConnectionStrings:DefaultConnection` | — (required) | PostgreSQL connection string |
| `Jwt:Key` | — (required, 32+ characters) | JWT signing key |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireHours` | `TravelAdvisor.Api`, `TravelAdvisor.Clients`, `24` | Token settings |
| `Cors:AllowedOrigins` | local client ports | Allowed browser origins |
| `Swagger:Enabled` | Development only | Swagger UI and JSON |
| `Seed:DemoUsers` | Development only | Seed demo accounts and catalog |
| `Seed:AdminEmail`, `Seed:AdminPassword` | — | Bootstrap a real admin account |
| `RateLimiting:Enabled` | `true` | Turn rate limiting on or off |
| `RateLimiting:AuthPermitsPerMinute` | `10` | Sign-in and registration requests per IP |
| `RateLimiting:AiPermitsPerMinute` | `20` | AI chat requests per user |
| `Ai:ApiKey`, `Ai:BaseUrl`, `Ai:Model` | empty, OpenAI, `gpt-4o-mini` | LLM provider |
| `Ai:MaxToolIterations`, `Ai:BookingProposalMinutes` | `6`, `15` | Tool rounds per turn, proposal lifetime |

Admins change runtime settings (currency, maintenance message, advance-booking window, cancellation cut-off,
AI on/off) through `PUT /api/settings/{key}` or the admin settings page.

## Demo accounts

Seeded when `Seed:DemoUsers` is true (Development by default). Do not enable this in production.

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@traveladvisor.com | Admin@123 |
| Hotel owner | owner@traveladvisor.com | Owner@123 |
| Travel agent | agent@traveladvisor.com | Agent@123 |
| Traveler | register in the React or Flutter app | — |

## Testing

All tests run against the real system; recorded results are in `testing/execution-results/`.

| Suite | Command | Notes |
|-------|---------|-------|
| Backend unit tests (xUnit, Moq, AI evaluation) | `dotnet test tests/TravelAdvisor.UnitTests` | The 35 live-LLM evaluation cases skip themselves unless the `AI_EVAL_API_KEY` environment variable holds a working key (optional: `AI_EVAL_BASE_URL`, `AI_EVAL_MODEL`) |
| API, database and security tests | `dotnet test tests/TravelAdvisor.Api.Tests` | Needs Docker (Testcontainers PostgreSQL) |
| React unit and component tests | `cd web-react && npm test && npm run lint && npm run typecheck` | Vitest, React Testing Library, MSW |
| Web end-to-end | `cd web-react && npm run e2e` | Playwright against the real API and database; starts the API on 5080 and Vite if they are not running. Set `E2E_DB_CONNECTION` for the database checks |
| Flutter | `cd mobile-app && flutter analyze && flutter test` | mocktail-based unit and widget tests |
| Flutter integration | see below | `integration_test` traveler journey against the running API |
| API contract | `cd testing/integration/postman && npm install && npm test` | Newman run of the Postman collection against `http://localhost:5080` |
| Performance | `testing/performance/README.md` | k6 in Docker |
| Security | `testing/security/README.md` | OWASP ZAP baseline and API scans in Docker |

Flutter integration test in Chrome (start ChromeDriver on port 4444 first, and the API on 5080):

```bash
cd mobile-app
flutter drive --driver=test_driver/integration_test.dart --target=integration_test/traveler_journey_test.dart \
  -d web-server --browser-name=chrome --profile --web-port 4173 --dart-define=API_BASE_URL=http://localhost:5080
```

The Flutter contract tests in `mobile-app/test_live/` also need a running API with demo data and a traveler
account, so they are not part of `flutter test`:

```bash
cd mobile-app
flutter test test_live --dart-define=API_BASE_URL=http://localhost:5080 --dart-define=LIVE_USER_EMAIL=you@example.com --dart-define=LIVE_USER_PASSWORD=...
```

The k6, Newman, Playwright and ZAP runs sign in many times from one IP, so they need raised limits, e.g.
`RateLimiting__AuthPermitsPerMinute=100000` and `RateLimiting__AiPermitsPerMinute=100000` on the API under test.
Restore the defaults afterwards.

Test documentation:

- Test-case catalogue: `testing/test-cases/TEST-CASES.md` (ID, feature, preconditions, steps, input, expected,
  actual, status, evidence). `powershell -File testing/scripts/verify-test-cases.ps1` checks that every evidence
  reference points to a recorded result with a matching outcome.
- Defect log: `testing/defect-reports/DEFECT-LOG.md` (severity, priority, steps, fix and retest result).
- Latest results: `testing/execution-results/phase9-nfr/SUMMARY.md`.

## Project structure

```
web-api/              ASP.NET Core Web API (controllers, pipeline)
shared/Core/          Entities, DTOs, enums, validators, interfaces
shared/Infrastructure/ EF Core, migrations, repositories, services, agentic AI
web-react/            React + TypeScript web app (all roles)
web-app/              ASP.NET Core MVC portal (owner, agent, admin)
mobile-app/           Flutter traveler app
tests/                .NET unit and API test projects
testing/              Test plan, cases, defects, k6, ZAP, Postman, results
database/             Historical SQL reference
documentation/        Architecture, API, database, demo
```
