# Agentic AI Travel Advisor & Booking System

Full-stack travel advisory platform with Flutter mobile app, ASP.NET Web API, MVC admin portal, and PostgreSQL.

## Project Structure

| Directory | Description |
|-----------|-------------|
| `shared/Core/` | Entities, DTOs, enums, interfaces |
| `shared/Infrastructure/` | EF Core DbContext, Identity, JWT, migrations |
| `web-api/` | REST API with JWT authentication |
| `web-app/` | MVC admin portal with role-based dashboards |
| `mobile-app/` | Flutter mobile application |
| `agentic-ai/` | Multi-agent AI system (future) |
| `database/` | SQL schema reference and seed scripts |
| `documentation/` | Architecture, API, and database docs |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) (local install)
- [Flutter SDK](https://flutter.dev/) (for mobile app)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Quick Start

### 1. Database

```sql
CREATE DATABASE travel_advisor;
```

Update the PostgreSQL password in `web-api/appsettings.json`, then apply migrations:

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

### 2. Web API

```bash
dotnet run --project web-api
```

- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/api/health

### 3. Admin Web Portal

```bash
dotnet run --project web-app
```

- Portal: http://localhost:7000
- Demo login: `admin@traveladvisor.com` / `Admin@123`

### 4. Flutter Mobile App

```bash
cd mobile-app
flutter pub get
flutter run
```

API base URL is auto-detected (`10.0.2.2:5000` on Android emulator, `localhost:5000` elsewhere).

## Demo Accounts

| Role | Email | Password | Portal |
|------|-------|----------|--------|
| Admin | admin@traveladvisor.com | Admin@123 | Web |
| Hotel Owner | owner@traveladvisor.com | Owner@123 | Web |
| Travel Agent | agent@traveladvisor.com | Agent@123 | Web |
| User | Register via mobile app | — | Mobile |

## Documentation

- [Architecture](documentation/ARCHITECTURE.md)
- [API Reference](documentation/API.md)
- [Database Setup](documentation/DATABASE.md)
