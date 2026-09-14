# Architecture

## System Overview

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Mobile App │────▶│   Web API   │────▶│  PostgreSQL │
│  (Flutter)  │     │  (ASP.NET)  │     │  Database   │
└─────────────┘     └──────┬──────┘     └─────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │  Agentic AI │
                    │   System    │
                    └─────────────┘
```

## Components

| Component | Technology | Purpose |
|-----------|------------|---------|
| Mobile App | Flutter | User-facing mobile experience |
| Web App | ASP.NET Core | API, business logic, data access |
| Agentic AI | Multi-agent system | Travel planning, recommendations, booking |
| Database | PostgreSQL | Persistent storage |

## Agents

- **TravelPlanningAgent** — End-to-end trip planning
- **RecommendationAgent** — Personalized destination and activity suggestions
- **BookingAgent** — Flight, hotel, and activity reservations
- **ItineraryAgent** — Day-by-day itinerary generation
