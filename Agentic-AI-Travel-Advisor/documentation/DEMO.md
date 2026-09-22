# Demo script (10 minutes)

Walk this path for a live demonstration. USER stays on Flutter (or APK). Owner, Agent, and Admin use the portal at `http://localhost:7000`.

## Start the stack

```bash
docker compose up --build
```

- API: http://localhost:5000/swagger
- Portal: http://localhost:7000
- Flutter emulator: API is `http://10.0.2.2:5000` (default). Physical phone: rebuild the APK with `--dart-define=API_BASE_URL=http://YOUR_LAN_IP:5000`.

## Demo accounts (portal)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@traveladvisor.com | Admin@123 |
| Hotel Owner | owner@traveladvisor.com | Owner@123 |
| Travel Agent | agent@traveladvisor.com | Agent@123 |

## Script

1. **Register / login (Flutter)** — Create a USER account. Email and password stay on the mobile app; the portal blocks USER logins.
2. **AI trip** — Open the AI tab and send: `Plan a 3-day trip to Ella under Rs. 50,000.` Confirm a suggested plan appears (catalog fallback works without an LLM key).
3. **Save itinerary** — Save from chat. Open Profile → Saved itineraries.
4. **Book** — Open the suggested hotel (or Explore → Hotels) and book a room for future dates.
5. **Owner confirm** — Portal as owner. Dashboard shows pending/revenue. Bookings → Confirm.
6. **Admin approve** — Portal as owner, add a new hotel (stays Pending, hidden from Flutter). Log in as admin → Hotels → Approve. Flutter Explore now lists it.
7. **Agent package** — Same flow for a new package (Pending until admin approves). Customer requests are pending package bookings on the Agent Bookings page.
8. **Illegal status** — Confirmed bookings cannot jump back to Pending; the API returns 400.

## Optional LLM

```bash
dotnet user-secrets set "Ai:ApiKey" "YOUR_KEY" --project web-api/TravelAdvisor.Api.csproj
```

Or set `AI_API_KEY` in `.env` when using Docker Compose.
