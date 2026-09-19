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
                    │   Service   │
                    └─────────────┘
```

## Components

| Component | Technology | Purpose |
|-----------|------------|---------|
| Mobile App | Flutter | User-facing mobile experience including AI chat |
| Web App | ASP.NET Core MVC | Owner / agent / admin portal |
| Web API | ASP.NET Core | REST API, auth, catalog, bookings, AI, itineraries |
| Agentic AI | 3-stage pipeline + catalog tools | Travel planning, recommendations, itineraries |
| Database | PostgreSQL | Persistent storage |

## AI request flow

```
User message
    → POST /api/ai/chat
    → AgenticAiService
    → TravelPlanningAgent (extract requirements)
    → RecommendationAgent + CatalogTools (PostgreSQL)
    → ItineraryAgent (day-by-day plan)
    → Chat reply + SuggestedPlan
    → POST /api/itineraries (Save itinerary)
```

If destination or budget is missing, the planning agent asks a follow-up instead of inventing a plan.

LLM calls use an OpenAI-compatible Chat Completions client (`Ai:BaseUrl`, `Ai:Model`, `Ai:ApiKey`). When no API key is configured, the same pipeline runs with a catalog-backed fallback extractor and deterministic ranking.

## Agents

- **TravelPlanningAgent** — Understands the request; extracts destination, budget, dates, travelers, interests, accommodation
- **RecommendationAgent** — Queries hotels, rooms, packages, and activities in PostgreSQL and ranks within budget
- **ItineraryAgent** — Builds a daily schedule and estimated cost from catalog matches

Booking remains a separate Flutter + `/api/bookings` flow (no book-from-chat in Day 3).
