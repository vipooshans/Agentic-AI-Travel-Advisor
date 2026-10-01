# Agentic AI Travel Advisor & Booking System

Full-stack travel advisory platform with Flutter mobile app, ASP.NET Web API, MVC admin portal, and PostgreSQL.

## Day 5 — Demo ready

- Automated tests (`dotnet test`) for booking rules plus API flows (auth, RBAC, hotels, packages, bookings, AI, itineraries)
- UI polish: empty states, friendlier errors, pull-to-refresh, compact Flutter nav, responsive portal sidebar
- Docker Compose for PostgreSQL + API + MVC portal
- Android APK with a configurable API URL for emulator, LAN, or ngrok

## Quick Start (Docker)

```bash
cp .env.example .env
docker compose up --build
```

- Portal: http://localhost:7000
- API / Swagger: http://localhost:5000/swagger
- Postgres (host): `localhost:5433` (mapped from container `5432` so it does not clash with a local Postgres on 5432)

Env vars (see `.env.example`): `POSTGRES_PASSWORD`, `JWT_KEY` / `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `AI_API_KEY` / `Ai__ApiKey`.

## Day 4 Features

- Flutter USER hub: edit profile, travel preferences, cancel pending bookings, resume AI chat history
- Hotel owner and travel agent portals: approval badges, pending booking requests, Confirm/Cancel/Complete, revenue stats
- Admin portal: users (create owner/agent, activate/deactivate), hotel/package approval, booking monitor, live reports
- Public catalog shows **Approved** hotels and packages only; new listings start as **Pending**

## Day 3 Features

- AI chat that extracts destination, budget, dates, travelers, interests, and stay preference
- Catalog-backed recommendations (hotels, rooms, packages, activities from PostgreSQL)
- Day-by-day itinerary generation with estimated cost
- Save itinerary from the Flutter chat screen

## Day 2 Features

- Browse destinations, hotels, and travel packages (Flutter)
- Create room and package bookings (Flutter)
- Hotel owner portal: manage hotels, rooms, availability, view bookings
- Travel agent portal: create packages, add activities, manage bookings
- Full REST API with RBAC

## Quick Start (local without Docker)

### 0. Secrets (required once)

No secrets are stored in the repository. The API refuses to start until these are set:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=travel_advisor;Username=postgres;Password=YOUR_PASSWORD" --project web-api/TravelAdvisor.Api.csproj
dotnet user-secrets set "Jwt:Key" "A_RANDOM_STRING_OF_AT_LEAST_32_CHARACTERS" --project web-api/TravelAdvisor.Api.csproj
```

Environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Key`) work too.

### 1. Database

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

### 2. Web API (seeds sample data on first run, including Sri Lanka catalog)

```bash
dotnet run --project web-api
```

Swagger: http://localhost:5000/swagger

Optional LLM (OpenAI-compatible). If `Ai:ApiKey` is empty, a catalog-backed fallback planner still answers queries such as “Plan a 3-day trip to Ella under Rs. 50,000.”

```bash
dotnet user-secrets set "Ai:ApiKey" "YOUR_KEY" --project web-api/TravelAdvisor.Api.csproj
```

Or set environment variable `Ai__ApiKey`. Swap `Ai:BaseUrl` / `Ai:Model` for Groq or Gemini’s OpenAI-compatible endpoint.

### 3. Admin Portal

```bash
dotnet run --project web-app
```

Portal: http://localhost:7000

### 3b. React Web App (all four roles)

Needs Node.js 22.12+ (Vitest 5 does not support Node 20; verified on Node 24). The dev server proxies `/api` to `VITE_API_PROXY_TARGET` (default `http://localhost:5080`; see `web-react/.env.example`).

```bash
dotnet run --project web-api --urls http://localhost:5080   # in one terminal
cd web-react
npm install
npm run dev        # http://localhost:5173
```

Travelers register in the app and use the AI assistant, bookings, payments, reviews and itineraries. Hotel owners, travel agents and admins get their own dashboards. `npm run build` writes `web-react/dist/`; `npm run preview` serves it on port 4173 with the same proxy.

### 4. Flutter Mobile App

```bash
cd mobile-app
flutter pub get
flutter run
```

Use the **AI** tab (or the Home card) to chat, then **Save itinerary**.

The mobile app is for travelers (`USER` accounts). Hotel owners, travel agents and admins are told to use the web app. The API address comes from `--dart-define=API_BASE_URL=...`. It defaults to `http://10.0.2.2:5000` on the Android emulator and `http://localhost:5000` elsewhere; for example, use `--dart-define=API_BASE_URL=http://localhost:5080` when the API runs with `dotnet run` on port 5080.

On Windows, the `&` in this repository's folder name breaks Gradle and the Flutter tool scripts. Map the project folder to a drive letter first, e.g. `subst T: "<path>\Agentic-AI-Travel-Advisor"`, then build from `T:\mobile-app`.

### 5. Android APK

From `mobile-app` (run `powershell -File ../scripts/setup-android.ps1` once if `android/` is missing):

```bash
# Emulator (default API http://10.0.2.2:5000)
flutter build apk --release

# Physical device on the same Wi-Fi as Docker
flutter build apk --release --dart-define=API_BASE_URL=http://YOUR_LAN_IP:5000
```

APK: `mobile-app/build/app/outputs/flutter-apk/app-release.apk`

Cleartext HTTP is allowed for classroom demos. Point `API_BASE_URL` at an ngrok HTTPS/HTTP URL if the phone is off the LAN.

### Tests

```bash
dotnet test tests/TravelAdvisor.UnitTests
dotnet test tests/TravelAdvisor.Api.Tests   # needs Docker for Testcontainers PostgreSQL
cd mobile-app && flutter analyze && flutter test
```

The Flutter contract tests in `mobile-app/test_live/` run against a live API with seeded demo data and a traveler account, and are not part of `flutter test`:

```bash
cd mobile-app && flutter test test_live --dart-define=API_BASE_URL=http://localhost:5080 --dart-define=LIVE_USER_EMAIL=you@example.com --dart-define=LIVE_USER_PASSWORD=...
```

From the repository root again:

```bash
cd web-react && npm test && npm run lint && npm run typecheck
```

## Demo Accounts

| Role | Email | Password | Platform |
|------|-------|----------|----------|
| Admin | admin@traveladvisor.com | Admin@123 | Web |
| Hotel Owner | owner@traveladvisor.com | Owner@123 | Web |
| Travel Agent | agent@traveladvisor.com | Agent@123 | Web |
| User | Register in the React app or on mobile | — | React web / Flutter |

## Documentation

- [Demo script](documentation/DEMO.md)
- [API Reference](documentation/API.md)
- [Database Setup](documentation/DATABASE.md)
- [Architecture](documentation/ARCHITECTURE.md)
