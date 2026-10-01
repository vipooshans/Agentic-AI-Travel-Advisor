# API Reference

ASP.NET Core 8 Web API for the Agentic AI Travel Advisor & Booking System. This document lists every
endpoint (74 operations across 15 controllers), who may call it, and the request and response contracts.
The live, always-current contract is the Swagger UI.

| Item | Value |
|------|-------|
| Base URL (`dotnet run --project web-api`, launch profile) | `http://localhost:5000` |
| Base URL used by the React dev proxy and the test tools | `http://localhost:5080` (`dotnet run --project web-api --urls http://localhost:5080`) |
| Base URL (Docker Compose) | `http://localhost:5000` |
| Swagger UI | `/swagger` (enabled when `Swagger:Enabled` is true; defaults to the Development environment) |
| Content type | `application/json`; errors use `application/problem+json` |
| Health check | `GET /api/health` |

## Authentication

1. `POST /api/auth/register` or `POST /api/auth/login` returns `{ token, expiresAt, user }`.
2. Send the token on every protected request: `Authorization: Bearer <token>`.
3. In Swagger, click **Authorize** and paste the token (without the `Bearer ` prefix).

Token rules:

- HS256 JWT, validated for issuer, audience, lifetime (1 minute clock skew) and signing key.
- Every request re-checks the account: a deactivated user, or a user whose role has changed since the
  token was issued, is rejected with 401 even if the token has not expired.
- Self-registration always creates a `USER`. `HOTEL_OWNER` and `TRAVEL_AGENT` accounts are created by an
  admin (`POST /api/users`). Admin accounts come from the seeder; the API cannot create them.
- Passwords need at least 6 characters with an upper-case letter, a lower-case letter, a digit and a symbol.
- Five failed logins lock the account for 5 minutes. A wrong password and an unknown email return the same
  401 message, so the API does not reveal which emails are registered.

### Roles and authorization policies

| Policy | Roles |
|--------|-------|
| Authenticated | any signed-in user |
| `RequireUser` | `USER` |
| `RequireHotelOwner` | `HOTEL_OWNER` |
| `RequireTravelAgent` | `TRAVEL_AGENT` |
| `RequireAdmin` | `ADMIN` |
| `HotelOwnerOrAdmin` | `HOTEL_OWNER`, `ADMIN` |
| `TravelAgentOrAdmin` | `TRAVEL_AGENT`, `ADMIN` |
| `ProviderOrAdmin` | `HOTEL_OWNER`, `TRAVEL_AGENT`, `ADMIN` |

Policies only decide who may call an endpoint. The services additionally check ownership: a hotel owner can
change only their own hotels and rooms, a travel agent only their own packages and transport, and a traveler
only their own bookings, reviews, itineraries and conversations. Calls on another provider's resource return
403; itineraries and AI conversations that belong to someone else return 404 (for every role, including admin).

## Errors

Every error is an RFC 7807 problem document with an extra `message` field that the clients show to users:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Room is already booked for the selected dates.",
  "instance": "/api/bookings",
  "message": "Room is already booked for the selected dates.",
  "traceId": "0HN..."
}
```

Validation failures (400) add an `errors` map of field name to messages. Unexpected exceptions return 500
with a generic message; stack traces and SQL details are never sent to the client.

| Status | Meaning |
|--------|---------|
| 400 | Validation failure or broken business rule (e.g. check-out before check-in, reviewing an uncompleted booking) |
| 401 | Missing, invalid or expired token, or the account was deactivated: "Authentication is required. Sign in and try again." |
| 403 | Authenticated but not allowed: "You do not have permission to perform this action." |
| 404 | Resource does not exist (or is hidden from the caller) |
| 409 | Conflict: overlapping booking, blocked room night, duplicate email or review, deleting a listing that has bookings, concurrent edit |
| 429 | Rate limit exceeded; the response carries a `Retry-After` header |
| 503 | The AI assistant is switched off by the admin setting `Ai.AssistantEnabled` |

### Rate limits

| Policy | Applies to | Default | Partition |
|--------|-----------|---------|-----------|
| `auth` | `POST /api/auth/register`, `POST /api/auth/login` | 10 requests per minute (fixed window) | client IP |
| `ai` | `POST /api/ai/chat` | 20 requests per minute (sliding window, 6 segments) | user ID, otherwise IP |

Configured with `RateLimiting:Enabled`, `RateLimiting:AuthPermitsPerMinute` and `RateLimiting:AiPermitsPerMinute`.

## Enumerations

Enums are sent and returned as integers.

| Enum | Values |
|------|--------|
| `ApprovalStatus` | 0 Pending, 1 Approved, 2 Rejected |
| `BookingStatus` | 0 Pending, 1 Confirmed, 2 Cancelled, 3 Completed |
| `PaymentMethod` | 0 Card, 1 Cash, 2 BankTransfer |
| `PaymentStatus` | 0 Pending, 1 Completed, 2 Refunded, 3 Failed |
| `ReviewStatus` | 0 Visible, 1 Hidden |
| `TransportMode` | 0 Bus, 1 Train, 2 Car, 3 Van, 4 TukTuk, 5 Flight, 6 Ferry |
| `ItineraryStatus` | 0 Draft, 1 Active, 2 Completed, 3 Archived |
| `RecommendationType` | 0 Hotel, 1 Package, 2 Activity, 3 Transportation |

## Endpoints

"Public" means no token is needed.

### Auth — `/api/auth`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| POST | `/register` | Public, `auth` rate limit | `{ email, password, firstName, lastName }`; 409 "Email is already registered." |
| POST | `/login` | Public, `auth` rate limit | `{ email, password }` |
| GET | `/me` | Authenticated | Current user |
| PUT | `/me` | Authenticated | `{ firstName, lastName }` |

### Users — `/api/users`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/me/profile` | Authenticated | `{ phoneNumber, nationality, dateOfBirth, avatarUrl, bio, preferredCurrency }` |
| PUT | `/me/profile` | Authenticated | Same shape |
| GET | `/me/preferences` | `RequireUser` | `{ budgetMin, budgetMax, preferredClimate, interests, accommodationPreference, transportPreference }`; the AI planner reads these |
| PUT | `/me/preferences` | `RequireUser` | Same shape |
| GET | `?role=` | `RequireAdmin` | List users, optionally by role |
| POST | `/` | `RequireAdmin` | Create a provider account: `role` must be `HOTEL_OWNER` or `TRAVEL_AGENT` |
| PATCH | `/{id}/active` | `RequireAdmin` | Activate or deactivate; a deactivated user's tokens stop working immediately. Admins cannot deactivate themselves |

### Destinations — `/api/destinations`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `?q=` | Public | Case-insensitive search on name, country and description |
| GET | `/{id}` | Public | |
| POST | `/` | `RequireAdmin` | |
| PUT | `/{id}` | `RequireAdmin` | |
| DELETE | `/{id}` | `RequireAdmin` | 409 when travel packages still reference it |

Destination names are unique per country (409 on a duplicate).

### Hotels — `/api/hotels`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `?q&city&country&approvalStatus&maxPrice&guests` | Public | Non-admins only see approved hotels; `approvalStatus` is honoured for admins |
| GET | `/mine` | `RequireHotelOwner` | The owner's hotels in every approval state |
| GET | `/{id}` | Public | Unapproved hotels are visible only to their owner and admins |
| POST | `/` | `RequireHotelOwner` | `{ name, address, city, country, description, imageUrl }`; starts as Pending |
| PUT | `/{id}` | `HotelOwnerOrAdmin` (own hotel) | A change made by the owner sends the hotel back to Pending for re-approval |
| DELETE | `/{id}` | `HotelOwnerOrAdmin` (own hotel) | 409 when it has bookings |
| PATCH | `/{id}/approval` | `RequireAdmin` | `{ status }` — must be Approved (1) or Rejected (2) |

### Rooms — `/api/hotels/{hotelId}/rooms`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/` | Public | |
| POST | `/` | `HotelOwnerOrAdmin` (own hotel) | `{ name, roomType, pricePerNight, capacity }` |
| PUT | `/{roomId}` | `HotelOwnerOrAdmin` (own hotel) | |
| PATCH | `/{roomId}/availability` | `HotelOwnerOrAdmin` (own hotel) | Take the whole room on or off sale |
| GET | `/{roomId}/calendar?from&to` | `HotelOwnerOrAdmin` (own hotel) | Per-night entries `{ date, isBlocked, priceOverride, note }` |
| PUT | `/{roomId}/calendar` | `HotelOwnerOrAdmin` (own hotel) | Save entries; blocked nights cannot be booked, price overrides change the quote |
| DELETE | `/{roomId}` | `HotelOwnerOrAdmin` (own hotel) | 409 when it has bookings; mark it unavailable instead |

### Travel packages — `/api/packages`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `?q&destinationId&approvalStatus&maxPrice&maxDurationDays` | Public | Non-admins only see approved packages |
| GET | `/mine` | `RequireTravelAgent` | |
| GET | `/{id}` | Public | Unapproved packages are visible only to their agent and admins |
| POST | `/` | `RequireTravelAgent` | `{ destinationId, title, description, price, durationDays, imageUrl, maxTravelers }`; starts as Pending |
| PUT | `/{id}` | `TravelAgentOrAdmin` (own package) | A change made by the agent sends the package back to Pending |
| DELETE | `/{id}` | `TravelAgentOrAdmin` (own package) | 409 when it has bookings |
| POST | `/{id}/activities` | `TravelAgentOrAdmin` (own package) | `{ title, description, dayNumber, price, sortOrder }` |
| DELETE | `/{packageId}/activities/{activityId}` | `TravelAgentOrAdmin` (own package) | |
| PATCH | `/{id}/approval` | `RequireAdmin` | `{ status }` — Approved (1) or Rejected (2) |

A package's price per traveler is its base price plus the price of every activity.

### Transportation — `/api/transportation`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `?from&to&destinationId&travelPackageId&mode&maxPrice` | Public | Active routes only |
| GET | `/mine` | `TravelAgentOrAdmin` | |
| GET | `/{id}` | Public | Inactive routes are visible only to their provider and admins |
| POST | `/` | `TravelAgentOrAdmin` | `{ travelPackageId?, destinationId?, mode, fromLocation, toLocation, departureTime ("HH:mm")?, durationMinutes, pricePerPerson, capacity, description, isActive }` |
| PUT | `/{id}` | `TravelAgentOrAdmin` (own route) | |
| DELETE | `/{id}` | `TravelAgentOrAdmin` (own route) | |

### Bookings — `/api/bookings`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/availability?roomId&travelPackageId&checkIn&checkOut&guests` | Public | Quote without booking: `{ available, reason, roomId, travelPackageId, checkIn, checkOut, nights, guests, totalPrice, remainingPlaces }` |
| POST | `/` | `RequireUser` | `{ roomId?, travelPackageId?, checkIn, checkOut?, guests, notes? }` — exactly one of `roomId` / `travelPackageId` |
| GET | `/` | Authenticated | Scoped by role: travelers see their own, owners see bookings on their hotels, agents on their packages, admins see all |
| GET | `/{id}` | Authenticated | Same scoping; 403 otherwise |
| PATCH | `/{id}/status` | Authenticated | `{ status }` — see the workflow below |

Booking rules enforced by the backend:

- The hotel or package must be approved (400) and the room must be on sale (409).
- Check-in cannot be in the past or more than `Booking.MaxAdvanceDays` (default 365) days ahead. Room
  bookings need a check-out after check-in and at most 30 nights. Guests must fit the room capacity or the
  package's maximum travelers.
- Bookings run in a transaction that locks the room or package row, and a PostgreSQL exclusion constraint
  blocks overlapping stays on the same room. Overlaps and blocked calendar nights return 409; when 8
  identical requests race, exactly one succeeds. A traveler cannot book the same package twice on one date (409).
- The total price is calculated on the server (nightly rate with calendar overrides, or package price per
  traveler times guests). Client-supplied prices are never used.

#### Booking status workflow

```
PENDING ──► CONFIRMED ──► COMPLETED
   │            │
   └──► CANCELLED ◄──┘
```

| Transition | Who may do it |
|-----------|---------------|
| Pending → Confirmed | Listing owner (hotel owner / travel agent) or admin |
| Pending → Cancelled | The traveler who booked, the listing owner or admin |
| Confirmed → Cancelled | Listing owner or admin; the traveler only while check-in is more than `Booking.GuestCancellationCutoffHours` away |
| Confirmed → Completed | Listing owner or admin, and only on or after the check-in date |

Every other transition (including anything out of Cancelled or Completed) returns 400. Cancelling a booking
refunds completed payments and voids pending ones.

### Payments — `/api/bookings/{bookingId}/payments`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/` | Authenticated (anyone who can see the booking) | |
| POST | `/` | `RequireUser` (own booking) | `{ method, cardNumber? }` — pays the outstanding amount of a Pending or Confirmed booking; 409 if it is already paid or a payment is awaiting confirmation |
| PATCH | `/{paymentId}/status` | `ProviderOrAdmin` (own listing) | `{ status }` — a Pending (cash / bank transfer) payment can be marked Completed (1) or Failed (3) |

Payments are simulated. Card payments complete immediately; cash and bank transfer payments stay Pending
until the provider confirms them. The card number must be 12–19 digits and pass the Luhn checksum; it is
never stored. The test card `4000 0000 0000 0002` is always declined, which records a Failed payment.

### Reviews

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/api/hotels/{hotelId}/reviews` | Public | Visible reviews; author shown as first name and last initial |
| GET | `/api/packages/{packageId}/reviews` | Public | |
| GET | `/api/reviews?status&hotelId&travelPackageId` | Authenticated | Travelers see their own reviews, owners and agents the visible reviews of their listings, admins all (with filters) |
| POST | `/api/reviews` | `RequireUser` | `{ bookingId, rating (1–5), comment }` — booking must be yours and Completed (400 otherwise); one review per booking (409) |
| PUT | `/api/reviews/{id}` | `RequireUser` (own review) | |
| DELETE | `/api/reviews/{id}` | Authenticated (author or admin) | |
| PATCH | `/api/reviews/{id}/status` | `RequireAdmin` | Moderation: `{ status }` 0 Visible / 1 Hidden |

### Itineraries — `/api/itineraries`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| POST | `/` | `RequireUser` | `{ title, startDate, endDate, destinationId?, estimatedCost?, budget?, travelers?, conversationId?, summary?, items: [{ dayNumber, title, description, startTime, sortOrder }] }` |
| GET | `/` | Authenticated | Own itineraries |
| GET | `/{id}` | Authenticated | 404 if it belongs to someone else |
| DELETE | `/{id}` | Authenticated | Own itinerary only |

### Reports — `/api/reports`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/summary` | Authenticated | Booking counts per status and revenue, scoped like `GET /api/bookings` |
| GET | `/statistics?from&to&top` | `ProviderOrAdmin` | `{ from, to, totalBookings, bookingsByStatus, revenue, averageBookingValue, cancellationRate, totalGuests, averageRating, reviewCount, monthly[], topListings[] }`; ranges are capped at 36 months |

### System settings — `/api/settings`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| GET | `/public` | Public | `{ defaultCurrency, aiAssistantEnabled, maintenanceMessage, maxAdvanceBookingDays, guestCancellationCutoffHours }` |
| GET | `/` | `RequireAdmin` | All settings |
| PUT | `/{key}` | `RequireAdmin` | `{ value }`. Only known keys can be set; anything else (e.g. `Jwt.Key`) returns 404 |

Known keys: `Platform.DefaultCurrency`, `Platform.MaintenanceMessage`, `Booking.MaxAdvanceDays`,
`Booking.GuestCancellationCutoffHours`, `Ai.AssistantEnabled`.

### Health — `/api/health`

| Method | Route | Access |
|--------|-------|--------|
| GET | `/` | Public |

## AI assistant — `/api/ai`

| Method | Route | Access | Notes |
|--------|-------|--------|-------|
| POST | `/chat` | `RequireUser`, `ai` rate limit | 503 when `Ai.AssistantEnabled` is false |
| GET | `/conversations` | Authenticated | Own conversations `{ id, title, updatedAt }` |
| GET | `/conversations/{id}` | Authenticated | Messages of an own conversation; 404 otherwise |
| GET | `/recommendations?conversationId` | Authenticated | Items the assistant recommended `{ itemType, hotelId, roomId, travelPackageId, transportationId, destinationId, title, estimatedCost, score, reason }` |

### Chat request

```json
{ "conversationId": null, "message": "Plan a 3 day trip to Ella for 2 people, budget 60000 LKR", "confirmBookingId": null }
```

Omit `conversationId` to start a new conversation; send the returned id to continue it.

### Chat response

| Field | Description |
|-------|-------------|
| `conversationId` | Conversation the turn was saved to |
| `message` | Assistant reply text (sanitized: no secrets, prompts or other users' data) |
| `status` | `plan`, `clarification`, `no_match`, `over_budget`, `booking_proposal`, `booking_created`, `booking_failed`, `refused` or `info` |
| `plan` | Structured, schema-validated travel plan built only from database tool results (hotels, packages, activities, transport, day-by-day items, estimated cost vs. budget) |
| `suggestedPlan` | Legacy single hotel/package plan kept for older clients |
| `pendingBooking` | A booking proposal awaiting confirmation: `{ id, kind ("room"/"package"), hotelId, roomId, travelPackageId, title, checkIn, checkOut, guests, quotedTotal, currency, expiresAt }`. Nothing is booked yet |
| `booking` | The booking exactly as the backend created it (normally status Pending) |
| `mode` | `llm` when an OpenAI-compatible model chose the tools, `deterministic` when the rule-based planner ran |
| `agents` | Agents that handled the turn (`TravelPlanningAgent`, `RecommendationAgent`, `ItineraryAgent`, `BookingAgent`) |
| `toolCalls` | `[{ name, success, error, durationMs }]` for every backend tool the turn used |

### Booking through the assistant

1. The traveler asks to book. The assistant checks availability with a backend tool and replies with
   `status: "booking_proposal"` and a `pendingBooking`.
2. The traveler confirms, either by sending `confirmBookingId` with the proposal id or by replying "confirm".
   Only the latest unexpired proposal of that conversation can be confirmed.
3. The backend creates the booking through the normal booking service (same validation and overlap rules).
   The response is `booking_created` with the real `booking`, or `booking_failed` with the reason.

The assistant never reports a booking as confirmed or completed on its own; it repeats the status the
backend returned. Attempts to override its instructions, or requests for secrets, the system prompt, other
users' data or admin privileges, are answered with `status: "refused"`.

## Example: create a booking

```http
POST /api/bookings
Authorization: Bearer <traveler token>
Content-Type: application/json

{ "roomId": 3, "checkIn": "2026-11-10", "checkOut": "2026-11-13", "guests": 2, "notes": "Late arrival" }
```

Response `201 Created` with the booking (`status: 0` Pending, server-calculated `totalPrice`). A second
request for an overlapping stay on the same room returns 409.
