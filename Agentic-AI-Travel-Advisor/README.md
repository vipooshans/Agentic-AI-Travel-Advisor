# Agentic AI Travel Advisor & Booking System

Travel advisory and booking platform with an agentic AI assistant:

- **Web API** — ASP.NET Core 8, EF Core, PostgreSQL, JWT authentication and role-based authorization for
  `USER`, `HOTEL_OWNER`, `TRAVEL_AGENT` and `ADMIN`.
- **Agentic AI** — an orchestrator with planning, recommendation, itinerary and booking agents that act only
  through schema-validated backend tools. It uses an OpenAI-compatible model with function calling when
  `Ai:ApiKey` is set, and a deterministic planner otherwise.
- **React web app** (`web-react`) — all four roles.
- **MVC portal** (`web-app`) — hotel owners, travel agents and admins.
- **Flutter app** (`mobile-app`) — travelers.

Booking workflow: `PENDING → CONFIRMED → COMPLETED`, with `CANCELLED` reachable from Pending and Confirmed.
The AI can propose a booking, but a booking is only created after the traveler confirms it, and it starts as
Pending until the provider confirms it.

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
| Backend unit tests (xUnit, Moq, AI evaluation) | `dotnet test tests/TravelAdvisor.UnitTests` | The live-LLM cases skip themselves unless a working `Ai:ApiKey` is configured |
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
