# Phase 9 - Non-functional testing

## 9a Performance (k6)

Scripts and how to run them: `testing/performance/README.md`. All numbers below come from the logs and summary files in this folder; nothing was re-typed from memory.

### Environment

- **Machine:** one laptop for everything: AMD Ryzen 7 250 (16 logical cores), 15.3 GB RAM, Windows 11.
  - The API ran with `dotnet run`, Debug build, .NET SDK 10.0.103 running the net8.0 app, Development environment.
  - PostgreSQL ran locally.
  - k6 v2.3.0 ran in Docker Desktop 29.2.0 (`grafana/k6`) against `http://host.docker.internal:5080`.
  - Because load generator, API and database share the CPU, these numbers show behaviour under load on a development machine. They are not production capacity figures.
- **Rate limits** were raised to 100,000 per minute for sign-in and AI (`RateLimiting__AuthPermitsPerMinute`, `RateLimiting__AiPermitsPerMinute`). All k6 traffic comes from one IP, so the default limits (10 sign-ins per minute per IP, 20 AI requests per minute per user) would otherwise have rejected almost every request with 429. The limiter itself is covered by the API tests.
- **Logging:** the per-request ASP.NET Core hosting log was lowered to Warning (`Logging__LogLevel__Microsoft.AspNetCore.Hosting.Diagnostics`).
- **Dataset:** 8 destinations, 31 hotels (all approved) with 75 rooms, and 8 travel packages (7 approved). The database held 2,366 bookings after all runs, most of them cancelled k6 bookings.
- **Load model:** closed. Each virtual user waits 1 s between iterations (2 s for AI), so throughput is set mainly by the number of users and the think time, not by the API's maximum capacity. "Concurrent users" below are k6 virtual users.

### Results

| Test (log) | Concurrent users | Requests | Throughput | Error rate | Response time avg / p95 / max | Thresholds |
|---|---|---|---|---|---|---|
| Catalog search (`k6-catalog-search.log`) | 10 → 25 → 50 | 33,877 | 132.8 req/s | 0.00 % (0 of 33,877) | 5.17 / 7.98 / 174.5 ms | all passed |
| Login + current user (`k6-login.log`) | 10 → 20 | 4,794 | 24.3 req/s (12.1 sign-ins/s) | 0.00 % | login 57.6 / 69.8 / 128.3 ms; current user 4.2 / 6.6 / 17.3 ms | all passed |
| Booking flow + double-booking race (`k6-booking.log`) | 10, then 20 at once | 4,733 | 35.8 req/s | 0.00 % in the flow (0 of 4,648) | create 12.1 / 16.5 / 32.8 ms | all passed |
| AI chat, deterministic planner (`k6-ai-chat.log`) | 5 → 10 | 573 | 3.35 req/s | 0.00 % | 28.2 / 35.0 / 88.6 ms | all passed |

Per request type, p95:
- **Catalog:** destinations 5.5 ms, hotel search 7.5 ms, package search 8.2 ms, hotel details 7.3 ms, availability quote 8.9 ms.
- **Booking:** quote 9.3 ms, create 16.5 ms, get 7.6 ms, cancel 10.6 ms.

Checks (correctness of each response, not just its status):
- **Catalog:** 40,650 of 40,650 passed, including "hotel search returns only approved hotels".
- **Login:** 9,548 of 9,548 passed (token returned, role USER).
- **Booking:** 6,992 of 6,992 passed. This includes the quote total of LKR 16,000 for 2 nights, and new bookings being Pending at the server's price.
- **AI:** 1,126 of 1,126 passed. Every answer was a structured plan with a total within the LKR 60,000 budget.

The request counts include the setup requests: 2 lookups for catalog, 20 registrations for login, 10 for AI, and for booking 2 sign-ins, 1 hotel, 31 rooms, 1 approval and 30 registrations.

**Double booking under concurrency.** 20 travelers requested the same room and dates at the same moment. Exactly 1 booking was created and the other 19 got `409 Conflict` (`race_bookings_created = 1`, `race_conflicts = 19`). The database confirms it (`k6-booking-room-usage.txt`, from `testing/performance/booking-room-usage.mjs`):
- The race room has one booking, still active.
- Each of the 10 flow travelers used only their own room.
- Their 1,162 bookings were all cancelled by the test.

### Runs that are kept but superseded

- **`k6-booking-run1.log` (script bug, not a product defect).** Run 1 had one flow room per expected virtual user. k6 numbers virtual users across both scenarios, though, so some flow users mapped to rooms already taken by another user. Rooms 1–3 each got 232 bookings from two travelers (`k6-booking-run1-room-usage.txt`).
  - Those users then competed for the same dates. 32 quotes correctly said "not available", and 9 bookings were correctly rejected with 409 (error rate 0.19 %, checks 99.41 %).
  - The thresholds still passed, but the run did not measure what it was meant to measure. The script now creates one room and one traveler per possible virtual-user number, and the run above was repeated.
- **`k6-ai-chat-rejected-key.log` (found DEF-026).** The local `Ai:ApiKey` user secret was a placeholder that OpenAI rejects with 401. Every chat request first waited for that rejection, then fell back to the deterministic planner. The results:
  - p95 527.9 ms, average 455.3 ms, maximum 1,541.7 ms over 478 requests.
  - 0 errors, and all 936 checks passed.
  - The API log showed one fallback warning per request.

  This was logged and fixed as DEF-026: the client now stops calling the provider for 10 minutes after a 401 or 403. The rerun above used the same placeholder key, and its whole run fell inside one suspension window: one rejected provider call before the run (the live retest probe), none during it. p95 dropped from 527.9 ms to 35.0 ms.

### Observations

- None of the endpoints came close to the thresholds. The slowest operation is sign-in, at a p95 of about 70 ms, because password hashing (PBKDF2) is deliberately expensive. That makes sign-in the part to watch first as concurrency grows.
- The occasional maximums of 160–300 ms were single slow requests, most likely JIT warm-up or garbage collection in the Debug build. Overall p99 was 10.2 ms for catalog, 79.8 ms for login (91.7 ms for the sign-in request alone), 53.0 ms for booking and 61.3 ms for AI.
- The AI numbers are for the deterministic planner. With a working model key, AI response time is dominated by the model provider and is not covered by these runs.
