# API Documentation

Base URL: `http://localhost:5000` | Swagger: `/swagger`

## Authentication

All protected endpoints require `Authorization: Bearer <token>`.

---

## Destinations

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/destinations` | Public | List all destinations |
| GET | `/api/destinations/{id}` | Public | Destination detail with package count |

## Hotels

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/hotels` | Public | List hotels (`?city=`, `?country=`) |
| GET | `/api/hotels/mine` | HOTEL_OWNER | Owner's hotels |
| GET | `/api/hotels/{id}` | Public | Hotel detail with rooms |
| POST | `/api/hotels` | HOTEL_OWNER | Create hotel |
| PUT | `/api/hotels/{id}` | HOTEL_OWNER | Update own hotel |

## Rooms

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/hotels/{hotelId}/rooms` | Public | List rooms |
| POST | `/api/hotels/{hotelId}/rooms` | HOTEL_OWNER | Add room |
| PUT | `/api/hotels/{hotelId}/rooms/{roomId}` | HOTEL_OWNER | Update room |
| PATCH | `/api/hotels/{hotelId}/rooms/{roomId}/availability` | HOTEL_OWNER | Toggle availability |

## Packages

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/packages` | Public | List packages (`?destinationId=`) |
| GET | `/api/packages/mine` | TRAVEL_AGENT | Agent's packages |
| GET | `/api/packages/{id}` | Public | Package detail with activities |
| POST | `/api/packages` | TRAVEL_AGENT | Create package |
| PUT | `/api/packages/{id}` | TRAVEL_AGENT | Update own package |
| POST | `/api/packages/{id}/activities` | TRAVEL_AGENT | Add activity |
| DELETE | `/api/packages/{packageId}/activities/{activityId}` | TRAVEL_AGENT | Remove activity |

## Bookings

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/bookings` | USER | Create booking (room or package) |
| GET | `/api/bookings` | Authenticated | Role-scoped booking list |
| GET | `/api/bookings/{id}` | Authenticated | Single booking |

### Create Booking Example

```json
POST /api/bookings
{
  "roomId": 1,
  "checkIn": "2026-10-01T00:00:00Z",
  "checkOut": "2026-10-05T00:00:00Z"
}
```

Package booking (checkOut auto-calculated from duration):

```json
{
  "travelPackageId": 1,
  "checkIn": "2026-10-01T00:00:00Z"
}
```

---

## Auth (Day 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register USER |
| POST | `/api/auth/login` | Login, returns JWT |
| GET | `/api/auth/me` | Current user |
| GET | `/api/health` | Health check |

---

## AI Chat (Day 3)

All AI endpoints require `Authorization: Bearer <token>`. Chat is restricted to the **USER** role.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/ai/chat` | USER | Send a message; returns assistant reply and optional suggested plan |
| GET | `/api/ai/conversations` | Authenticated | List current user's conversations |
| GET | `/api/ai/conversations/{id}` | Authenticated | Conversation history (messages + suggested plans) |

### Chat Example

```json
POST /api/ai/chat
{
  "conversationId": null,
  "message": "Plan a 3-day trip to Ella under Rs. 50,000."
}
```

Response includes `conversationId`, `message`, and `suggestedPlan` (hotel, package, estimated cost, day items) when destination and budget are known. Incomplete prompts return a clarifying question and no plan.

Pass `conversationId` on follow-up messages to continue the same thread.

---

## Itineraries (Day 3)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/itineraries` | USER | Save a travel plan (title, dates, items, optional destination/cost) |
| GET | `/api/itineraries` | Authenticated | Current user's saved itineraries |
| GET | `/api/itineraries/{id}` | Authenticated | Detail with day items |

### Save Itinerary Example

```json
POST /api/itineraries
{
  "title": "3-Day Ella Trip",
  "startDate": "2026-10-03T00:00:00Z",
  "endDate": "2026-10-05T00:00:00Z",
  "destinationId": 6,
  "estimatedCost": 44000,
  "summary": "Ella Gap View Inn + Ella Hills Escape",
  "items": [
    { "dayNumber": 1, "title": "Nine Arch Bridge", "description": "Morning visit", "startTime": "09:00:00", "sortOrder": 0 }
  ]
}
```
