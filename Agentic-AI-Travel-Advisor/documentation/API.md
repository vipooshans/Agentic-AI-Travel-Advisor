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
