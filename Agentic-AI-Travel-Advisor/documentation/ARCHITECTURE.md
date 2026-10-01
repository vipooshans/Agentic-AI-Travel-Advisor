# Architecture

## System overview

```
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│ React web app    │   │ MVC portal       │   │ Flutter app      │
│ (all 4 roles)    │   │ (owner/agent/    │   │ (travelers)      │
│ Vite + TS        │   │  admin, cookies) │   │                  │
└────────┬─────────┘   └────────┬─────────┘   └────────┬─────────┘
         │ JSON + JWT           │ JSON + JWT           │ JSON + JWT
         ▼                      ▼                      ▼
┌──────────────────────────────────────────────────────────────────┐
│ ASP.NET Core 8 Web API (web-api)                                 │
│ controllers → services → repositories / unit of work → EF Core   │
│ agentic AI: orchestrator, agents, tool registry, safety layer    │
└───────────────┬───────────────────────────────┬──────────────────┘
                │                               │ optional
                ▼                               ▼
        ┌───────────────┐            ┌──────────────────────────┐
        │ PostgreSQL 16 │            │ OpenAI-compatible LLM    │
        └───────────────┘            │ (function calling)       │
                                     └──────────────────────────┘
```

Every client talks only to the Web API. No client touches the database, and the LLM never touches the
database either: it can only ask the API to run a whitelisted tool.

## Repository layout

| Path | Contents |
|------|----------|
| `web-api/` | `TravelAdvisor.Api`: controllers, `Program.cs` pipeline, exception handler, validation filter |
| `shared/Core/` | `TravelAdvisor.Core`: entities, enums, DTOs, FluentValidation validators, service and repository interfaces, app exceptions |
| `shared/Infrastructure/` | `TravelAdvisor.Infrastructure`: `ApplicationDbContext`, migrations, seeder, repositories, unit of work, services, agentic AI, DI registration |
| `web-react/` | React 19 + TypeScript + Vite + React Router + Axios web app |
| `web-app/` | ASP.NET Core MVC portal for hotel owners, travel agents and admins |
| `mobile-app/` | Flutter traveler app (Provider, go_router, http, flutter_secure_storage) |
| `tests/` | `TravelAdvisor.UnitTests` (xUnit + Moq) and `TravelAdvisor.Api.Tests` (WebApplicationFactory + Testcontainers PostgreSQL) |
| `testing/` | Test plan, test-case catalogue, defect log, k6, OWASP ZAP, Postman/Newman, AI evaluation, recorded results |
| `database/` | Historical SQL reference; the schema is owned by EF Core migrations |
| `documentation/` | This document, API reference, database guide, demo script |
| `agentic-ai/` | Empty placeholder folders from the original scaffold; the AI lives in `shared/Infrastructure/AI` |

## Backend

### Layers

```
HTTP request
  → Controller          routing, [Authorize] policies, reads the caller from the JWT
  → Service             business rules, ownership checks, transactions
  → Repository / UoW    EF Core queries, row locks, SaveChanges
  → ApplicationDbContext → PostgreSQL
```

- Controllers stay thin and never use the `DbContext` directly.
- Services receive a `UserContext` (user id and role) and enforce ownership. Failures are thrown as typed
  exceptions (`NotFoundException` 404, `ForbiddenException` 403, `ConflictException` 409,
  `BusinessRuleException` 400, `ServiceUnavailableException` 503).
- `IUnitOfWork.ExecuteInTransactionAsync` wraps multi-step writes (bookings, payments) in a database
  transaction and rolls back and clears the change tracker on any failure.
- An `AuditInterceptor` on the `DbContext` stamps `CreatedAt` and `UpdatedAt`.
- Request DTOs are validated by FluentValidation through a global validation filter before the action runs.

### Request pipeline (`web-api/Program.cs`)

1. Security headers on every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
   `Referrer-Policy: no-referrer`, `Cross-Origin-Resource-Policy: same-origin`, and outside `/swagger` also
   `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` and `Cache-Control: no-store`.
2. Exception handler: `ApiExceptionHandler` turns exceptions into RFC 7807 problem documents. PostgreSQL
   exclusion, unique and foreign-key violations become 409, check violations 400, everything else a generic 500.
3. Swagger UI with a Bearer security scheme (when `Swagger:Enabled`).
4. CORS policy `AllowClients` from `Cors:AllowedOrigins`.
5. HTTPS redirection, unless the API listens only on HTTP or runs in the Testing environment.
6. Authentication (JWT bearer) → Authorization (role policies) → rate limiter.
7. Controllers.
8. At startup, `DbSeeder` applies EF Core migrations, seeds roles and system settings, optionally bootstraps
   an admin (`Seed:AdminEmail` / `Seed:AdminPassword`), and seeds demo accounts and catalog when
   `Seed:DemoUsers` is true (default: Development only).

### Security

| Concern | Measure |
|---------|---------|
| Secrets | No secrets in the repository. The API refuses to start without `ConnectionStrings:DefaultConnection` or with a `Jwt:Key` shorter than 32 characters. Locally they come from user secrets, in Docker from `.env` (gitignored) |
| Authentication | ASP.NET Core Identity (PBKDF2 hashes), password policy, lockout after 5 failures for 5 minutes, identical error for wrong password and unknown email |
| Tokens | HS256 JWT validated for issuer, audience, lifetime and key. `IUserStatusCache` re-checks on each request that the account is active and the role unchanged |
| Authorization | Role policies on every endpoint, plus ownership checks in the services (providers only touch their own listings, travelers their own bookings, reviews, itineraries and conversations) |
| Role escalation | Registration always creates `USER`; client-supplied role, status, owner and price fields are ignored; new and edited listings need admin approval |
| Abuse | Rate limits on sign-in/registration (per IP) and AI chat (per user) |
| Injection | EF Core LINQ everywhere; the only raw SQL is parameterized `SELECT … FOR UPDATE` row locks |
| Error handling | Problem documents with a user-facing `message`; no stack traces or SQL in responses |

Not implemented: `Strict-Transport-Security` (HSTS). For production it should be added with `UseHsts` or at
the TLS-terminating proxy.

### Domain rules

- **Approval workflow.** Hotels and packages start as Pending. Only approved listings appear in public search,
  AI tools and bookings. A change made by the owner or agent sends the listing back to Pending.
- **Bookings.** Created as Pending inside a transaction that locks the room or package row. A PostgreSQL
  exclusion constraint (`EX_Bookings_Room_NoOverlap`, room × stay range, for bookings that are not cancelled)
  is the final guard against double booking.
  Prices are computed on the server, including per-night calendar overrides; blocked nights cannot be booked.
- **Status workflow.** Pending → Confirmed → Completed, with Cancelled reachable from Pending and Confirmed.
  Providers and admins confirm and complete; guests can cancel Pending bookings, and Confirmed ones only
  before the cancellation cut-off (`Booking.GuestCancellationCutoffHours`). Cancelling refunds or voids payments.
- **Payments.** Simulated gateway: card payments complete at once (test card `4000 0000 0000 0002` is declined),
  cash and bank transfer wait for the provider. Card numbers are validated and never stored.
- **Reviews.** One per completed booking, by its guest; admins can hide reviews.
- **Settings.** A fixed whitelist of system settings (currency, maintenance message, advance-booking window,
  cancellation cut-off, AI on/off) editable by admins.

## Agentic AI

All AI code is in `shared/Infrastructure/AI`. `POST /api/ai/chat` goes through `AgenticAiService`, which loads
the conversation, runs the `AgentOrchestrator`, validates the structured plan, saves the turn and any
recommendations, and returns a `ChatResponse`.

### Turn flow

```
message
  → PromptInjectionGuard ── blocked ──► status "refused"
  → confirmation of a pending proposal? ──► BookingAgent.ConfirmAsync → CreateBooking tool → BookingService
  → LLM configured and not suspended?
        yes → LLM tool loop (max Ai:MaxToolIterations rounds, max 6 tool calls per round)
              any failure → fall back ▼
        no  → deterministic agents (TravelPlanningAgent → GenerateItinerary tool → RecommendationAgent + ItineraryAgent)
  → output checks: system-prompt leak, unverified booking claim, redaction
  → ChatResponse { status, message, plan, pendingBooking, booking, mode, agents, toolCalls }
```

### Agents

| Agent | Role |
|-------|------|
| `TravelPlanningAgent` | Extracts destination, dates, duration, travelers, budget (converted to LKR), interests, accommodation and transport preference from the conversation; asks a clarifying question when destination or budget is missing |
| `RecommendationAgent` | Ranks approved hotels, rooms, packages, activities and transport from the catalog within the budget |
| `ItineraryAgent` | Builds the day-by-day plan and the cost breakdown |
| `BookingAgent` | Turns "book the hotel / package" into a priced proposal and, after confirmation, creates the booking through the booking service |

### Tools

The LLM (or the deterministic agents) can only act through these tools. Each declares a JSON Schema for its
arguments, which `ToolRegistry` validates before running it, and the roles allowed to use it. Tools the caller
may not use are not even offered to the LLM.

| Tool | Allowed roles | Backend source |
|------|---------------|----------------|
| `SearchDestinations` | any | catalog queries |
| `SearchHotels` | any | approved hotels and rooms |
| `SearchTravelPackages` | any | approved packages with activities |
| `SearchActivities` | any | activities of approved packages |
| `SearchTransportation` | any | active transport routes |
| `CheckAvailability` | any | `BookingService` availability quote |
| `GetTravelPreferences` | `USER` | the caller's saved preferences |
| `GenerateItinerary` | any | Recommendation + Itinerary agents |
| `CreateBooking` | `USER` | creates a proposal; books only for a confirmed proposal |

### Safety measures

- **Injection guard.** `PromptInjectionGuard` blocks instruction overrides, system-prompt requests, secret
  and data exfiltration and privilege escalation before any model or tool call.
- **Grounding.** Plans are built from tool results only and validated against a JSON Schema
  (`TravelPlanSchema`). LLM replies that quote prices no tool returned are replaced.
- **Budget.** The planner keeps the plan within the stated budget, or says `over_budget` and shows the cheapest
  real option, or `no_match` when nothing fits.
- **Booking confirmation.** A booking needs an explicit confirmation of the latest unexpired proposal
  (`Ai:BookingProposalMinutes`, default 15). A forged or reused proposal id books nothing. The created booking
  goes through `BookingService`, so all booking rules apply and it starts as Pending.
- **Output checks.** `OutputSanitizer` replaces replies that leak the system prompt or claim a booking the
  backend did not return, and redacts private keys, JWTs, connection strings, passwords, API keys, bearer
  tokens and email addresses.
- **Privacy.** Conversations, recommendations and itineraries are visible only to their owner (404 for anyone
  else, including admins).

### LLM and deterministic modes

- With `Ai:ApiKey` set, `OpenAiCompatClient` calls any OpenAI-compatible Chat Completions endpoint
  (`Ai:BaseUrl`, `Ai:Model`, default `gpt-4o-mini`) with function calling. The response has `mode: "llm"`.
- Without a key, or when the model call fails, the deterministic planner answers (`mode: "deterministic"`)
  using the same tools and catalog.
- `LlmAvailability`: when the provider rejects the key with 401 or 403, LLM calls are suspended for 10 minutes so
  every chat turn does not wait for a doomed request (DEF-026).
- Admins can switch the assistant off with the `Ai.AssistantEnabled` setting; chat then returns 503.

## Clients

### React web app (`web-react`)

- One app for all four roles. `RequireRole` guards the routes per role (`pages/public`, `pages/user`,
  `pages/owner`, `pages/agent`, `pages/admin`, `pages/shared`); the API still enforces every rule.
- `src/api/client.ts` is an Axios instance that adds the bearer token, converts problem documents into
  readable errors, and on an unexpected 401 clears the session and sends the user to sign in.
- The session (token, expiry, user) is kept in `localStorage` and dropped when it expires.
- Travelers: search, hotel and package details, availability quotes, bookings, payments, reviews, itineraries,
  profile and preferences, and the AI assistant with plan view and booking confirmation.
  Owners and agents: listings, rooms and calendars, packages and activities, transport, bookings, statistics.
  Admins: approvals, users, destinations, review moderation, settings, statistics.
- The Vite dev server proxies `/api` to `VITE_API_PROXY_TARGET` (default `http://localhost:5080`).

### MVC portal (`web-app`)

Server-rendered portal for hotel owners, travel agents and admins. It signs in through the API, keeps the JWT
in an authentication cookie and forwards it with `JwtCookieHandler` on every `TravelApiClient` call
(`ApiSettings:BaseUrl`). It has no database access of its own.

### Flutter app (`mobile-app`)

Traveler app: destinations, hotels, packages, reviews and transport, bookings, AI chat and conversation history,
itineraries, profile and preferences. State uses Provider, navigation go_router, and the token is kept in
`flutter_secure_storage`. Accounts with other roles are told to use the web app. A 401 from the API signs the
user out. The API address comes from `--dart-define=API_BASE_URL`.

## Database

PostgreSQL 16 accessed through EF Core 8 (Npgsql). The schema is defined by the entity configurations and
the migrations in `shared/Infrastructure/Migrations`, and applied at API startup. Main tables: Identity users
and roles, user profiles and travel preferences, destinations, hotels, rooms, room availability (calendar),
travel packages and activities, transportation, bookings, payments, reviews, AI conversations and messages,
AI recommendations, itineraries and items, system settings. Constraints include foreign keys, unique
indexes, check constraints and the booking exclusion constraint. See [DATABASE.md](DATABASE.md).

## Deployment

`docker-compose.yml` runs PostgreSQL (host port 5433), the API (host port 5000, Production) and the MVC portal
(host port 7000). Secrets come from `.env` (`POSTGRES_PASSWORD`, `JWT_KEY`, optional `AI_API_KEY`). Demo
accounts are seeded only with `SEED_DEMO_USERS=true`.

## Testing

| Level | Tooling | Location |
|-------|---------|----------|
| Unit | xUnit, Moq | `tests/TravelAdvisor.UnitTests` |
| API, database, security | WebApplicationFactory, Testcontainers PostgreSQL | `tests/TravelAdvisor.Api.Tests` |
| AI evaluation | xUnit scenario suite (categories A–J), optional live-LLM cases | `tests/TravelAdvisor.UnitTests/AI`; report in `testing/execution-results/phase8-tests/ai-evaluation` |
| React | Vitest, React Testing Library, MSW | `web-react/src/**/*.test.tsx` |
| Web end-to-end | Playwright against the real API and database | `web-react/e2e` |
| Flutter | flutter_test, mocktail, integration_test | `mobile-app/test`, `mobile-app/integration_test` |
| API contract | Postman collection run with Newman | `testing/integration/postman` |
| Performance | k6 | `testing/performance` |
| Security | OWASP ZAP baseline and API scans | `testing/security` |

The test-case catalogue (`testing/test-cases/TEST-CASES.md`) links every case to recorded evidence in
`testing/execution-results/`, and `testing/scripts/verify-test-cases.ps1` checks that every reference resolves.
Defects and their retests are in `testing/defect-reports/`.
