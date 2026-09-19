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

Public list/detail return **Approved** hotels only. New hotels are `Pending` until an admin approves them. Editing a rejected hotel resubmits it as `Pending`.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/hotels` | Public (Approved); ADMIN sees all (`?approvalStatus=`) | List hotels (`?city=`, `?country=`) |
| GET | `/api/hotels/mine` | HOTEL_OWNER | Owner's hotels (all approval states) |
| GET | `/api/hotels/{id}` | Public if Approved; owner/admin otherwise | Hotel detail with rooms |
| POST | `/api/hotels` | HOTEL_OWNER | Create hotel (`Pending`) |
| PUT | `/api/hotels/{id}` | HOTEL_OWNER | Update own hotel (Rejected → Pending) |
| PATCH | `/api/hotels/{id}/approval` | ADMIN | `{ "status": 1 }` Approved or `{ "status": 2 }` Rejected |

## Rooms

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/hotels/{hotelId}/rooms` | Public | List rooms |
| POST | `/api/hotels/{hotelId}/rooms` | HOTEL_OWNER | Add room |
| PUT | `/api/hotels/{hotelId}/rooms/{roomId}` | HOTEL_OWNER | Update room |
| PATCH | `/api/hotels/{hotelId}/rooms/{roomId}/availability` | HOTEL_OWNER | Toggle availability |

## Packages

Same approval rules as hotels. Public catalog is Approved-only.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/packages` | Public (Approved); ADMIN sees all (`?approvalStatus=`) | List packages (`?destinationId=`) |
| GET | `/api/packages/mine` | TRAVEL_AGENT | Agent's packages (all approval states) |
| GET | `/api/packages/{id}` | Public if Approved; agent/admin otherwise | Package detail with activities |
| POST | `/api/packages` | TRAVEL_AGENT | Create package (`Pending`) |
| PUT | `/api/packages/{id}` | TRAVEL_AGENT | Update own package (Rejected → Pending) |
| PATCH | `/api/packages/{id}/approval` | ADMIN | `{ "status": 1 \| 2 }` Approve or Reject |
| POST | `/api/packages/{id}/activities` | TRAVEL_AGENT | Add activity |
| DELETE | `/api/packages/{packageId}/activities/{activityId}` | TRAVEL_AGENT | Remove activity |

## Bookings

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/bookings` | USER | Create booking (room or package); listing must be Approved |
| GET | `/api/bookings` | Authenticated | Role-scoped booking list |
| GET | `/api/bookings/{id}` | Authenticated | Single booking |
| PATCH | `/api/bookings/{id}/status` | Guest / listing owner / agent / ADMIN | `{ "status": Confirmed\|Cancelled\|Completed }` |

Allowed transitions: Pending → Confirmed (owner/agent/admin); Pending → Cancelled (guest or staff); Confirmed → Completed or Cancelled (owner/agent/admin). Illegal jumps return 400.

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
| PUT | `/api/auth/me` | Update first/last name |
| GET | `/api/health` | Health check |

## Users, preferences, reports (Day 4)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/users` | ADMIN | List users (`?role=USER\|HOTEL_OWNER\|TRAVEL_AGENT\|ADMIN`) |
| POST | `/api/users` | ADMIN | Create HOTEL_OWNER or TRAVEL_AGENT |
| PATCH | `/api/users/{id}/active` | ADMIN | `{ "isActive": false }` activate/deactivate |
| GET | `/api/users/me/preferences` | USER | Travel preferences |
| PUT | `/api/users/me/preferences` | USER | `{ budgetMin, budgetMax, preferredClimate, interests }` |
| GET | `/api/reports/summary` | Authenticated | Role-scoped counts and revenue (Confirmed + Completed `TotalPrice`) |

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
