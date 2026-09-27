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

### 4. Flutter Mobile App

```bash
cd mobile-app
flutter pub get
flutter run
```

Use the **AI** tab (or the Home card) to chat, then **Save itinerary**.

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
cd mobile-app && flutter test
```

## Demo Accounts

| Role | Email | Password | Platform |
|------|-------|----------|----------|
| Admin | admin@traveladvisor.com | Admin@123 | Web |
| Hotel Owner | owner@traveladvisor.com | Owner@123 | Web |
| Travel Agent | agent@traveladvisor.com | Agent@123 | Web |
| User | Register via mobile | — | Flutter |

## Documentation

- [Demo script](documentation/DEMO.md)
- [API Reference](documentation/API.md)
- [Database Setup](documentation/DATABASE.md)
- [Architecture](documentation/ARCHITECTURE.md)
