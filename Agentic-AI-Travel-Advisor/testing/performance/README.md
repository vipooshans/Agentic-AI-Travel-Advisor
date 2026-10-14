# Performance tests (k6)

Load tests for the real API, written for [k6](https://k6.io) and run in the official `grafana/k6` Docker image, so nothing needs to be installed except Docker.

| Script | What it does | Load | Pass criteria (thresholds) |
|---|---|---|---|
| `k6/catalog-search.js` | Anonymous browsing: destinations, hotel search, package search, hotel details, availability quote, 1 s think time | ramps 10 → 25 → 50 virtual users over 4 min 15 s | errors < 1 %, checks > 99 %, p95 < 500 ms per request type |
| `k6/login.js` | Sign in, then `GET /api/auth/me`, 1 s think time (20 traveler accounts registered in setup) | ramps 10 → 20 virtual users over 3 min 15 s | errors < 1 %, checks > 99 %, login p95 < 1000 ms, current user p95 < 300 ms |
| `k6/booking.js` | `booking_flow`: each traveler quotes, books, reads and cancels a stay on their own room, 1 s think time. `double_booking_race`: 20 travelers book the same room and dates at the same moment | 10 virtual users for 2 min, then 20 simultaneous requests | flow errors < 1 %, checks > 99 %, p95 < 1000 ms; exactly 1 race booking created and 19 rejected with 409 |
| `k6/ai-chat.js` | `POST /api/ai/chat` asking for a 3-day plan within LKR 60,000; checks a structured plan within budget, 2 s think time | ramps 5 → 10 virtual users over 2 min 50 s | errors < 1 %, checks > 99 %, p95 < 2000 ms |

`k6/lib.js` holds the shared helpers. Setup steps create their own data with a per-run id: traveler accounts `k6.<script>.<run>.<n>@example.test`, and for the booking test a hotel "k6 Hotel <run>" with 31 rooms, created by the hotel owner and approved by the admin. Runs never depend on data left by another run.

## Running

1. Start the API on port 5080 (see the main README). The default limits are 10 sign-ins per minute per IP and 20 AI requests per minute per user. Every k6 request comes from the same IP, so raise these limits for the load test, or the test measures the rate limiter instead of the API:

   ```powershell
   $env:RateLimiting__AuthPermitsPerMinute = '100000'
   $env:RateLimiting__AiPermitsPerMinute = '100000'
   ${env:Logging__LogLevel__Microsoft.AspNetCore.Hosting.Diagnostics} = 'Warning'   # optional: no log line per request
   dotnet run --project web-api --no-build --urls http://localhost:5080 --environment Development
   ```

2. Run a script (from `Agentic-AI-Travel-Advisor/`):

   ```powershell
   .\testing\performance\run-k6.ps1 -Script catalog-search   # or login, booking, ai-chat
   ```

   The container reaches the host API at `http://host.docker.internal:5080` (`-BaseUrl` overrides this). The booking test signs in as the hotel owner and the admin. It uses the Development demo accounts unless `OWNER_EMAIL`, `OWNER_PASSWORD`, `ADMIN_EMAIL` and `ADMIN_PASSWORD` are set.

3. Results are written to `testing/execution-results/phase9-nfr/`:
   - `k6-<script>.log`: the console summary, with a ✓ or ✗ for each threshold.
   - `k6-<script>-summary.json`: every metric. In this file, a threshold value of `false` means the threshold was **not** crossed, i.e. it passed. k6 also exports the return value of `setup()`, which holds the access tokens of the run's traveler accounts. `run-k6.ps1` removes it with `redact-k6-summary.ps1` and leaves the metrics unchanged.

   `run-k6.ps1` exits with k6's exit code: 0 when every threshold passed, 99 when any failed.

4. After `booking.js`, `node testing/performance/booking-room-usage.mjs [hotelId]` lists the bookings per room of the newest k6 hotel, read-only. It shows whether each traveler stayed on their own room and whether exactly one race booking is still active. It needs `E2E_DB_CONNECTION`, set the same way as for the Playwright database checks.

## What the AI test measures

With no `Ai:ApiKey`, or one the provider rejects, the API answers with its deterministic planner. The AI test then measures the orchestrator, the catalog tools and the database, not a language model. After a rejected key, the API stops calling the provider for 10 minutes (DEF-026), so a run shorter than that makes at most one provider call. With a working key, response times depend on the model provider, and the rate limits and costs of that provider apply.
