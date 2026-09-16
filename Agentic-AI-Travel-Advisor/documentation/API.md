# API Documentation

## Base URL

| Environment | URL |
|-------------|-----|
| HTTP (dev) | `http://localhost:5000` |
| HTTPS (dev) | `https://localhost:5001` |

Swagger UI: `http://localhost:5000/swagger`

## Authentication

JWT Bearer token. Include in requests:

```
Authorization: Bearer <token>
```

---

## Endpoints

### Health Check

```
GET /api/health
```

**Response 200:**
```json
{
  "status": "healthy",
  "database": "connected",
  "timestamp": "2026-09-14T12:00:00Z"
}
```

---

### Register

```
POST /api/auth/register
Content-Type: application/json
```

**Body:**
```json
{
  "email": "user@example.com",
  "password": "Password@1",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response 200:**
```json
{
  "token": "eyJhbG...",
  "expiresAt": "2026-09-15T12:00:00Z",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "USER"
  }
}
```

Registration always assigns the `USER` role.

---

### Login

```
POST /api/auth/login
Content-Type: application/json
```

**Body:**
```json
{
  "email": "admin@traveladvisor.com",
  "password": "Admin@123"
}
```

**Response 200:** Same shape as register response.

---

### Current User

```
GET /api/auth/me
Authorization: Bearer <token>
```

**Response 200:**
```json
{
  "id": "uuid",
  "email": "admin@traveladvisor.com",
  "firstName": "System",
  "lastName": "Admin",
  "role": "ADMIN"
}
```

---

## Authorization Policies

| Policy | Required Role |
|--------|---------------|
| `RequireAdmin` | ADMIN |
| `RequireHotelOwner` | HOTEL_OWNER |
| `RequireTravelAgent` | TRAVEL_AGENT |
| `RequireUser` | USER |

---

## CORS

Allowed dev origins (configured in `web-api/appsettings.json`):

- `http://localhost:5000`, `https://localhost:5001`
- `http://localhost:7000`, `https://localhost:7001`
- `http://localhost:3000`
