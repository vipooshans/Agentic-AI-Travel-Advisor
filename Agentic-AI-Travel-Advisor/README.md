# Agentic AI Travel Advisor & Booking System

Full-stack travel advisory platform with Flutter mobile app, ASP.NET Web API, MVC admin portal, and PostgreSQL.

## Day 2 Features

- Browse destinations, hotels, and travel packages (Flutter)
- Create room and package bookings (Flutter)
- Hotel owner portal: manage hotels, rooms, availability, view bookings
- Travel agent portal: create packages, add activities, manage bookings
- Full REST API with RBAC

## Quick Start

### 1. Database

```bash
dotnet ef database update \
  --project shared/Infrastructure/TravelAdvisor.Infrastructure.csproj \
  --startup-project web-api/TravelAdvisor.Api.csproj
```

### 2. Web API (seeds sample data on first run)

```bash
dotnet run --project web-api
```

Swagger: http://localhost:5000/swagger

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

## Demo Accounts

| Role | Email | Password | Platform |
|------|-------|----------|----------|
| Admin | admin@traveladvisor.com | Admin@123 | Web |
| Hotel Owner | owner@traveladvisor.com | Owner@123 | Web |
| Travel Agent | agent@traveladvisor.com | Agent@123 | Web |
| User | Register via mobile | — | Flutter |

## Documentation

- [API Reference](documentation/API.md)
- [Database Setup](documentation/DATABASE.md)
- [Architecture](documentation/ARCHITECTURE.md)
