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

## 9b Security (OWASP ZAP + security test suites)

How to run the scans: `testing/security/README.md`.
- **Tool and target:** ZAP `zaproxy/zap-stable` (weekly image, 7 weeks old on 2026-10-01). It scanned the API in Development with the rate limits raised as in 9a, at `http://host.docker.internal:5080`.
- **Coverage:** the API scans imported all 73 operations of `/swagger/v1/swagger.json` and reached 126 URLs.
- **Reports:** `testing/security/zap/reports/`. **Console logs:** this folder.

### ZAP runs

| Run | Log | Result |
|---|---|---|
| Baseline (passive), before fix | `zap-baseline-before-def027.log` | 66 rules passed. 1 warning: Non-Storable Content (10049) on the three 404 pages `/`, `/robots.txt`, `/sitemap.xml` |
| API active scan, no token, before fix | `zap-api-before-def027.log` | 117 passed. 1 warning: **Cross-Origin-Resource-Policy header missing (90004)** on 7 responses. This was logged as DEF-027 |
| API active scan as USER, first attempt | `zap-api-user-run1-token-not-applied.log` | **Not a valid authenticated run.** Its 63 responses were 401, and the account's data was unchanged, so ZAP never sent the token. The runner was corrected (it no longer sets `ZAP_AUTH_HEADER_SITE`), and the run was repeated |
| API active scan as USER, before fix | `zap-api-user-before-def027.log` | 116 passed. 2 warnings: CORP missing (12 responses, DEF-027), and Timestamp Disclosure (10096) on `/api/auth/me` |
| Baseline, after fix | `zap-baseline.log` | 66 passed. 1 warning: the same Non-Storable Content on 2 of the 404 pages |
| API active scan, no token, after fix | `zap-api.log` | **118 passed, 0 warnings, 0 failures** |
| API active scan as USER, after fix | `zap-api-user.log` | **118 passed, 0 warnings, 0 failures** |

The final USER scan really ran as a signed-in user:
- Admin and provider endpoints answered **403** (38 responses).
- ZAP's template-injection payloads replaced the scan account's name. They were stored as plain text, and none was executed.

Across all runs, no scan received a 5xx response.

**The active rules that passed include:**
- SQL injection: generic, plus time-based for PostgreSQL, MySQL, MsSQL, Oracle and Hypersonic.
- Cross-site scripting: reflected, persistent and DOM.
- Remote OS command injection, path traversal, remote file inclusion, server-side template injection, XXE, CRLF injection, Log4Shell and Spring4Shell.
- Information disclosure (debug error messages, application error disclosure, PII, private IP), and the `.env` and hidden-file finders.

### Triage of the findings

| Finding | Risk (ZAP) | Decision |
|---|---|---|
| Cross-Origin-Resource-Policy header missing (90004) | Low | **Fixed (DEF-027).** `Cross-Origin-Resource-Policy: same-origin` is now sent on every response. The rerun has no warning |
| Timestamp Disclosure - Unix (10096) on `/api/auth/me` | Low | **False positive caused by the test.** The number ZAP found, `1790872104`, was the Unix time the runner had put into the scan account's email (`zap.user.1790872104@example.test`), which `/api/auth/me` echoes. The runner now uses a random suffix, and the warning is gone in the rerun |
| Non-Storable Content (10049) | Informational | **By design.** The API does not serve `/`, `/robots.txt` or `/sitemap.xml`. Every API response carries `Cache-Control: no-store` so that personal data is not cached |
| "A Client Error response code was returned" | Informational | **Expected.** Without a token: 401 on protected endpoints. As USER: 403 on admin and provider endpoints and on another user's booking (`GET /api/bookings/10`), 404 for ZAP's made-up ids, and 400 for invalid input |
| Authentication Request Identified | Informational | Expected: ZAP identified `POST /api/auth/login` and `POST /api/users` |

Stored payloads are not executed by any client. None of the clients renders server data as HTML:
- `web-react/src` has no `dangerouslySetInnerHTML` or `innerHTML`.
- The MVC views have no `Html.Raw`.
- The Flutter app has no HTML or WebView widgets.

### Security requirements and their evidence

Test names below are in `backend-api-tests-final.trx` and `backend-unit-tests-final.trx` (this folder, all passed). The Newman, Playwright and AI evaluation evidence is in `../phase8-tests/`.

| Requirement | Evidence |
|---|---|
| Authentication | `Login_demo_accounts_succeed_and_bad_password_fails`, `Wrong_password_and_unknown_email_both_return_401_with_the_same_message`, `Password_policy_boundaries`, `Account_locks_after_five_failed_logins`, `Login_endpoint_is_rate_limited_when_enabled`, `Login_email_is_case_insensitive` |
| Authorization and protected endpoints | `Every_protected_endpoint_enforces_its_role_policy` (every endpoint × every role), `Public_catalog_endpoints_do_not_require_a_token`; ZAP without a token: 401 on 63 requests to protected endpoints |
| A USER calling an admin endpoint gets 403 | `Normal_user_cannot_call_admin_apis`; ZAP as USER: 403 on `GET /api/users`, `PATCH /api/users/{id}/active`, `PUT /api/settings/{key}`, approvals and destination CRUD |
| JWT and invalid tokens | `Invalid_expired_and_unsigned_tokens_are_rejected`, `Malformed_authorization_headers_are_rejected`, `Deactivated_user_token_is_rejected_immediately`; Newman tampered-token request; Playwright "a tampered token is rejected by the API" |
| Role escalation | `Role_supplied_at_registration_is_ignored`, `Provider_cannot_self_approve_a_new_listing`, `Client_supplied_status_price_and_owner_are_ignored_when_booking` |
| Unauthorized resource access (IDOR) | `User_cannot_read_or_change_another_users_booking`, `Owner_cannot_touch_another_owners_rooms_or_bookings`, `Agent_cannot_modify_another_agents_package_but_admin_can`, `Provider_cannot_edit_another_providers_hotel_but_admin_can`, `Itineraries_and_conversations_are_private_even_from_admins`; ZAP as USER: 403 on another user's booking |
| SQL injection | ZAP SQL injection rules 40018–40027 passed in all scans. Data access uses EF Core LINQ. The only raw SQL is three `SELECT … FOR UPDATE` row locks in `BookingRepository`, and they use `ExecuteSqlInterpolatedAsync`, which sends the ids as parameters |
| XSS | ZAP XSS rules 40012, 40014, 40016, 40017 and 40026 passed. JSON responses carry `nosniff` and `Content-Security-Policy: default-src 'none'`, and clients encode all output (see above) |
| Sensitive data exposure | `Unknown_resource_returns_problem_details_without_internals`, `Validation_errors_return_problem_details_with_message_and_errors`, `Responses_carry_security_headers`; the Newman collection-level check for secrets and stack traces on all 79 requests; ZAP information-disclosure rules passed |
| AI does not leak secrets, prompts or other users' data | `Injection_and_exfiltration_attempts_are_refused`, `Recommendations_are_saved_and_private_to_their_owner`, `Conversation_keeps_plan_and_proposal_and_is_private`, `OutputSanitizerTests`; AI evaluation categories in `../phase8-tests/ai-evaluation/ai-evaluation-report.md` |
| AI does not claim a booking it did not make | `Booking_needs_confirmation_creates_one_pending_booking_and_cannot_be_replayed`, `Forged_confirmation_id_books_nothing` |

### Limits of this testing

- **ZAP's generated values.** ZAP fills parameters from the OpenAPI schema (ids such as 10, strings such as "q"). Most writes therefore stop at validation or "not found", so deep business flows are not exercised by the scan. Those flows are covered by the API tests, Newman and Playwright.
- **No admin or provider scan.** The active scan was not run as ADMIN, HOTEL_OWNER or TRAVEL_AGENT, to protect the shared development database. Those roles' access rules are covered by the access-control matrix and the ownership tests.
- **HTTP only.** The scans used plain HTTP in Development, so transport security was not tested. The API redirects to HTTPS when it listens on an HTTPS URL, but it does not send `Strict-Transport-Security`. For a production deployment, HSTS should be added at the API (`UseHsts`) or at the TLS-terminating proxy. This was not changed here, because it cannot be verified in this HTTP-only setup.
