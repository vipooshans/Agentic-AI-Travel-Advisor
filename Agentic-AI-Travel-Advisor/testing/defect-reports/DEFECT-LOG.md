# Defect Log

Severity: Critical / High / Medium / Low. Priority: P1 (fix now) / P2 (fix this release) / P3 (when possible).
Status: Open -> Fixed -> Retested (Closed) | Deferred.

Defects DEF-001 to DEF-016 were found by code inspection on 2026-10-01 before implementation began.

---

## DEF-001 Secrets committed to source control
- **Severity / Priority:** Critical / P1
- **Description:** The PostgreSQL password and the JWT signing key are hard-coded in `web-api/appsettings.json`, `web-app/appsettings.json`, `docker-compose.yml` defaults, `.env.example`, and the API test fixture.
- **Steps to reproduce:** Open `web-api/appsettings.json` at commit `4833828`, lines 10 and 13.
- **Expected:** No secrets in tracked files; configuration supplied via user-secrets or environment variables.
- **Actual:** Database password and JWT key are in plain text and in git history.
- **Evidence:** `web-api/appsettings.json@4833828`.
- **Status:** Retested (Closed)
- **Fix:** Removed secrets from all tracked files. `docker-compose.yml` now requires `POSTGRES_PASSWORD` and `JWT_KEY` from `.env` (git-ignored); `.env.example` holds placeholders. Local development reads `Jwt:Key` and the connection string from .NET user-secrets. A new random JWT key was generated (rotation). The API now refuses to start if `Jwt:Key` is shorter than 32 characters or the connection string is empty. The test fixture generates a random JWT key per run. The old values remain in git history; the history was not rewritten (Decision 3), so the old database password must be changed on any server that used it.
- **Retest result:** 2026-10-01 - `git grep` for the old database password and the old JWT key prefix over tracked files returns no matches; `dotnet test` passes (19 unit, 8 API). (The search terms themselves are deliberately not written here; an earlier version of this entry quoted them and was redacted on 2026-10-01.)

## DEF-002 Concurrent room bookings can double-book the same dates
- **Severity / Priority:** High / P1
- **Description:** `BookingHelper.ValidateAndPriceAsync` checks for overlap, then the controller inserts in a separate step with no transaction, lock, or database constraint.
- **Steps to reproduce:** Send two simultaneous `POST /api/bookings` requests for the same room and dates.
- **Expected:** Exactly one succeeds; the other gets 409 Conflict.
- **Actual:** Both can succeed.
- **Evidence:** `shared/Infrastructure/Helpers/BookingHelper.cs` lines 43-50 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** Two layers.
  - `BookingService.CreateAsync` runs inside `IUnitOfWork.ExecuteInTransactionAsync`. It first takes `SELECT ... FOR UPDATE` on the room (or package) row, so concurrent requests queue up and each one's overlap check sees the bookings committed before it.
  - The `EX_Bookings_Room_NoOverlap` exclusion constraint (added in Phase 2) is the database-level backstop. A `23P01` violation maps to 409.
- **Retest result:** Phase 3 (`testing/execution-results/phase3-booking/`).
  - `Concurrent_requests_for_the_same_room_and_dates_create_exactly_one_booking`: 8 parallel requests gave 1 × 201 and 7 × 409. It passed in the full run and 5/5 repeat runs.
  - `Database_exclusion_constraint_rejects_overlapping_rows_inserted_directly`: a raw SQL insert gets `23P01`.

## DEF-003 Deactivated users keep working JWTs
- **Severity / Priority:** High / P1
- **Description:** Token validation does not check `IsActive`; a deactivated user keeps access for up to 24 hours.
- **Steps to reproduce:** Log in as an owner, have Admin deactivate the account, call `GET /api/hotels/mine` with the old token.
- **Expected:** 401 Unauthorized.
- **Actual:** 200 OK.
- **Evidence:** `shared/Infrastructure/DependencyInjection.cs` JWT setup has no `OnTokenValidated` hook.
- **Status:** Retested (Closed)
- **Fix:** JwtBearer `OnTokenValidated` now checks the user's `IsActive` flag and current role through `IUserStatusCache` (30 s memory cache). `UserService.SetActiveAsync` invalidates the cache entry and rotates the security stamp, so the old token stops working on the next request. Tokens whose role claim no longer matches the database are also rejected.
- **Retest result:** 2026-10-01 - `SecurityHardeningTests.Deactivated_user_token_is_rejected_immediately` and `Invalid_expired_and_unsigned_tokens_are_rejected` pass (`testing/execution-results/phase1-backend-arch/api-tests.trx`).

## DEF-004 Login brute force is possible
- **Severity / Priority:** High / P2
- **Description:** `CheckPasswordSignInAsync(user, password, false)` disables lockout and there is no rate limiting.
- **Steps to reproduce:** Send repeated wrong-password logins for one account.
- **Expected:** Account locks or requests are throttled.
- **Actual:** Unlimited attempts accepted.
- **Evidence:** `web-api/Controllers/AuthController.cs` line 81 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** Identity lockout enabled (5 failed attempts, 5-minute lockout) and `AuthService.LoginAsync` calls `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`. Login and register are also limited to 10 requests per minute per IP (`RateLimiting:*` settings, 429 with `Retry-After`).
- **Retest result:** 2026-10-01 - `SecurityHardeningTests.Account_locks_after_five_failed_logins` and `Login_endpoint_is_rate_limited_when_enabled` pass (`testing/execution-results/phase1-backend-arch/api-tests.trx`).

## DEF-005 Login email comparison is case-sensitive
- **Severity / Priority:** Medium / P2
- **Description:** Login uses `u.Email == request.Email`, which PostgreSQL compares case-sensitively.
- **Steps to reproduce:** Log in as `ADMIN@TravelAdvisor.com` with the correct password.
- **Expected:** 200 OK with JWT.
- **Actual:** 401 Unauthorized.
- **Evidence:** `web-api/Controllers/AuthController.cs` line 74 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** `AuthService.LoginAsync` looks the user up with `UserManager.FindByEmailAsync`, which compares the normalized (upper-case) email.
- **Retest result:** 2026-10-01 - `SecurityHardeningTests.Login_email_is_case_insensitive` passes (`testing/execution-results/phase1-backend-arch/api-tests.trx`).

## DEF-006 Booking dates and duplicates are not validated
- **Severity / Priority:** Medium / P2
- **Description:** Past check-in dates are accepted, a missing check-in becomes `DateTime.MinValue`, room capacity is ignored, and identical package bookings can be created repeatedly.
- **Steps to reproduce:** `POST /api/bookings` with `checkIn` = yesterday, or the same package and date twice.
- **Expected:** 400 Bad Request / 409 Conflict.
- **Actual:** 201 Created.
- **Evidence:** `BookingHelper.cs` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:**
  - `CreateBookingRequestValidator` requires check-in and limits guests to 1-50.
  - Dates are normalised to UTC dates.
  - `BookingService` rejects:
    - check-in before today
    - check-in beyond `Booking.MaxAdvanceDays` (system setting, default 365)
    - stays over 30 nights
    - guests above room capacity or package `MaxTravelers`
    - blocked calendar nights
    - package dates whose remaining places are fewer than the party size
    - a second active booking by the same user for the same package and date (409)
- **Retest result:** Phase 3 (`testing/execution-results/phase3-booking/`). These tests passed:
  - `Room_booking_rejects_past_too_far_too_long_and_over_capacity`
  - `Package_capacity_duplicates_and_group_pricing_are_enforced`
  - `Concurrent_package_bookings_never_exceed_max_travelers` (6 parallel, 3 places: 3 × 201, 3 × 409)
  - `Blocked_nights_and_price_overrides_from_the_room_calendar_are_enforced`

## DEF-007 AI plan can exceed the user's budget
- **Severity / Priority:** Medium / P2
- **Description:** When no package fits, `RecommendationAgent` falls back to the cheapest over-budget package; estimates ignore activity prices and traveler count.
- **Steps to reproduce:** Ask "Plan a 3-day trip to Ella under Rs. 5,000".
- **Expected:** Plan stays within budget or clearly says no in-budget option exists.
- **Actual:** Over-budget package recommended.
- **Evidence:** `shared/Infrastructure/AI/Agents/RecommendationAgent.cs` lines 44-65 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:**
  - `BudgetPlanner` prices every combination the same way `BookingService` does:
    - packages: (price + every activity) × travelers
    - rooms: rate × nights × rooms needed
    - transport: return fare × travelers
  - It only returns combinations whose total is within the budget. When nothing fits, it returns `over_budget` with the cheapest real option instead of a plan.
  - Alternatives are only listed if swapping them in keeps the plan within budget.
  - `TravelPlanSchema` rejects any plan whose total exceeds the budget, or whose selected items or cost breakdown don't add up to the total. `ItineraryAgent` never returns a plan that fails this check.
  - Selected rooms and packages are re-priced with a live `checkAvailability` quote. A room that costs more on the chosen dates and would break the budget is dropped.
- **Retest result:** Phase 5 (`testing/execution-results/phase5-agentic-ai/`). These tests passed:
  - **Unit:**
    - `BudgetPlannerTests.Def007_package_that_would_break_the_budget_is_not_selected`: the Ella package costs 72,000 for two, so on a 50,000 budget no package is chosen.
    - `Selection_and_alternatives_never_exceed_the_budget`: 8 budgets × 4 group sizes × 4 durations.
    - `Package_total_includes_activities_for_every_traveler`
    - `Too_small_budget_reports_the_cheapest_real_option`
    - `TravelPlanSchemaTests.Over_budget_plan_is_rejected`
  - **API (seeded catalog, Testcontainers):**
    - `AiAgentTests.Ella_plan_is_structured_budget_compliant_and_schema_valid`
    - `Budget_too_small_returns_over_budget_without_a_plan`: "budget 5000 rupees" returns `over_budget` and no plan.

## DEF-008 Budget parser misses budgets and mislabels currency
- **Severity / Priority:** Medium / P2
- **Description:** The first regex match for a keyword is rejected if small (e.g. "max 4 people") and later matches are never tried; USD/$ amounts are treated as LKR; "travellers" is not recognised.
- **Steps to reproduce:** "Trip to Ella, max 4 people, budget Rs. 60000".
- **Expected:** budget = 60000, travelers = 4.
- **Actual:** budget = null (assistant asks for a budget).
- **Evidence:** `TravelPlanningAgent.ParseBudget` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** New `RequirementParser`:
  - **Budget:** every amount in the message is scored (currency marker +2, budget keyword +1). Numbers followed by a non-money unit (people, days, nights, stars and so on) are skipped. It understands `k`, `lakh`, `million` and comma formats.
  - **Currency:** `$`/USD, EUR and GBP budgets are converted to LKR at the configured `Ai:LkrPerUnit` rate, and the conversion is stated as an assumption.
  - **Travelers:** recognises "travellers"/"travelers", number words, "family of N", couple and solo.
  - When the user sends several messages, the latest message wins for each field.
- **Retest result:** Phase 5:
  - **Unit:** `RequirementParserTests` passed, 32 cases, including:
    - `Def008_people_count_is_not_read_as_budget`
    - `Foreign_currency_is_converted_with_a_stated_assumption`
    - `Numbers_that_are_not_money_are_ignored`
  - **API:** `AiAgentTests.Def008_group_size_is_not_mistaken_for_budget` passed. "max 4 people, budget Rs. 60000 to Kandy for 2 nights" produced a plan with 4 travelers, a 60,000 budget and 3 days.
  - **Manual:** the same request against the dev database (`manual-smoke-dev.log`) gave travelers=4, budget=60000, total=28000.

## DEF-009 MVC portal returns 500 on expired token or API 404
- **Severity / Priority:** Medium / P2
- **Description:** `GetFromJsonAsync` throws on non-success status codes; no 401 handling redirects to login.
- **Steps to reproduce:** Let the API token expire (or restart the API with a new key) and open `/Owner/Hotels`.
- **Expected:** Redirect to login.
- **Actual:** HTTP 500 error page.
- **Evidence:** `web-app/Services/TravelApiClient.cs` at `4833828`. Reproduced by `tests/TravelAdvisor.UnitTests/WebPortal/PortalApiErrorTests.cs` (portal in-process, API replaced by a stub): API 401 on `/Owner/Hotels` and `/Admin/Users`, 404 on `/Owner/EditHotel/999` and 403 on `/Owner/Rooms/5` all returned 500 (`testing/execution-results/phase8-tests/def009-before-fix.log`: 4 failed, 1 passed; the passing test is the control that a real API 500 stays an error).
- **Status:** Retested (Closed)
- **Fix:** `TravelApiClient` reads through one helper. A 404 on a read returns null or an empty list, which the controllers already show as Not Found. 401 throws `ApiUnauthorizedException` and 403 throws `ApiForbiddenException`, on reads and writes; other failures still throw. A global MVC filter (`web-app/Middleware/ApiAuthExceptionFilter.cs`) handles those two exceptions. On 401 it signs the user out, clearing both the portal cookie and `access_token`, and redirects to `/Account/Login` (with `ReturnUrl` for GET requests only). On 403 it redirects to `/Account/AccessDenied`. Login still uses its own call, so a wrong password keeps showing the form error.
- **Retest result:**
  - **Automated:** `PortalApiErrorTests` 7/7 passed (`def009-after-fix.log`). These are the 5 tests above, plus 2 added with the fix: a real form post (with its antiforgery token) whose API call gets 401 redirects to the login page without a return URL, and the client reports 400 as `false`/null, 404 as null/empty, and 401/403 as the new exceptions. Full unit suite: 404 passed, 0 failed, 35 skipped (live-LLM cases) (`backend-unit-tests-def009.log`).
  - **Manual, real API:** the portal on :7000 against the API on :5080 (`testing/scripts/def009-portal-retest.ps1`, output in `def009-manual-retest.log`). The owner signs in through the login form, and `/Owner/Hotels` gives 200. `/Owner/EditHotel/999999` gives 404. With the `access_token` cookie replaced by an invalid token, `/Owner/Hotels` gives 302 to `/Account/Login?ReturnUrl=%2FOwner%2FHotels`, the auth and token cookies are gone, and following the redirect shows the login page (200) instead of looping back to the dashboard.

## DEF-010 Approved listings can be edited without re-approval
- **Severity / Priority:** Medium / P3
- **Description:** Editing an approved hotel or package keeps `ApprovalStatus = Approved`.
- **Steps to reproduce:** Owner edits an approved hotel's description.
- **Expected:** Material edits return the listing to Pending.
- **Actual:** Stays Approved.
- **Evidence:** `HotelsController.Update`, `PackagesController.Update` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** `Ownership.StatusAfterEdit` in `CatalogServices.cs` covers hotel and package updates, and adding or removing a package activity. When a provider changes any listing field, the listing returns to Pending. A save with no changes, or an edit by an admin, keeps the current status.
- **Retest result:** Phase 3. `Provider_edits_send_approved_listings_back_for_review_but_admin_edits_do_not` passed. It covers an unchanged save, an admin edit and an owner edit; after the owner edit the hotel is publicly 404 and booking is refused, and adding an activity sets the package to Pending.

## DEF-011 Admin bypass in provider update endpoints is unreachable
- **Severity / Priority:** Low / P3
- **Description:** Update actions check `!IsAdmin()` but the endpoint policy only admits HOTEL_OWNER / TRAVEL_AGENT, so admins can never edit listings.
- **Steps to reproduce:** Admin calls `PUT /api/hotels/{id}`.
- **Expected:** Admin moderation allowed (or the dead code removed).
- **Actual:** 403 Forbidden.
- **Evidence:** `HotelsController.cs` line 137 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** New `HotelOwnerOrAdmin` and `TravelAgentOrAdmin` policies on update/delete/room/activity endpoints; ownership is enforced in the service layer (`Ownership.EnsureOwnerOrAdmin`), so other providers still get 403.
- **Retest result:** 2026-10-01 - `SecurityHardeningTests.Provider_cannot_edit_another_providers_hotel_but_admin_can` passes (`testing/execution-results/phase1-backend-arch/api-tests.trx`).

## DEF-012 Swagger, migrations and demo seeding run in Production
- **Severity / Priority:** Low / P2
- **Description:** Swagger is enabled in every non-Testing environment and demo users with known passwords are always seeded.
- **Steps to reproduce:** `docker compose up`, browse `/swagger`, log in as `admin@traveladvisor.com / Admin@123`.
- **Expected:** Configurable; disabled by default in Production.
- **Actual:** Always on.
- **Evidence:** `web-api/Program.cs` lines 25-38 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** `Swagger:Enabled` and `Seed:DemoUsers` settings, both defaulting to on only in the Development environment. Docker Compose reads them from `SWAGGER_ENABLED` / `SEED_DEMO_USERS`. A real admin can be bootstrapped with `Seed:AdminEmail` / `Seed:AdminPassword` instead of the demo accounts.
- **Retest result:** 2026-10-01 - `SecurityHardeningTests.Swagger_is_disabled_outside_development_by_default` passes (the test host runs in the `Testing` environment). Demo seeding remains on in tests because the fixture sets `Seed:DemoUsers=true` explicitly.

## DEF-013 AI errors are silently swallowed
- **Severity / Priority:** Low / P3
- **Description:** `catch {}` blocks in `AgenticAiService` and `TravelPlanningAgent` hide LLM failures with no logging.
- **Steps to reproduce:** Configure an invalid `Ai:ApiKey` and chat.
- **Expected:** Fallback used and a warning logged.
- **Actual:** Fallback used, nothing logged.
- **Evidence:** `AgenticAiService.cs` lines 73, 102 at `4833828`.
- **Status:** Retested (Closed)
- **Fix:**
  - The empty `catch {}` blocks are gone; no `catch {}` remains under `shared/Infrastructure/AI`.
  - `AgentOrchestrator` logs `LLM orchestration failed (...); falling back to the deterministic planner` as a warning, with the exception, then answers with the deterministic agents.
  - Tool failures are logged by `ToolRegistry`:
    - unexpected exceptions as errors
    - schema rejections and role denials as warnings
  - A plan that fails schema validation is logged as an error and not shown.
  - The user only ever gets a generic message, never the exception text.
- **Retest result:** Phase 5:
  - **Unit:** `AgentOrchestratorTests.Llm_failure_falls_back_to_the_deterministic_planner` passed. The scripted LLM throws `HttpRequestException` (401). The test checks:
    - the response comes from the deterministic planner
    - exactly one warning containing "falling back" is logged, carrying the exception
    - "401" does not appear in the reply
  - **Manual:** the API was run with `Ai__ApiKey=invalid-test-key` and `Ai__BaseUrl=http://127.0.0.1:9/v1`. Chat returned 200 with `mode=deterministic` and a plan, and the console logged the warning with the connection-refused exception (`def013-llm-unreachable-retest.log`).

## DEF-014 Flutter app: wrong-role login, no 401 handling, missing timeouts
- **Severity / Priority:** Low / P2
- **Description:** Owner/agent/admin accounts can log in to the traveler app and then hit 403s; expired tokens do not log out; only the AI call has a timeout.
- **Steps to reproduce:** Log in to the app as `owner@traveladvisor.com`, open Bookings.
- **Expected:** Clear message that the app is for travelers.
- **Actual:** "Failed to load bookings".
- **Evidence:** `mobile-app/lib/providers/auth_provider.dart`, `services/api_service.dart` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:**
  - `AuthService.login` rejects any role other than `USER` before storing the token, with the message "This app is for travelers. Hotel owners, travel agents and administrators should sign in to the web app." `restoreSession` discards a stored staff session the same way.
  - `ApiService` calls `onUnauthorized` on a 401 for a request that carried a token (sign-in and register are excluded). `AuthProvider` then clears storage and shows "Your session has expired. Please sign in again." on the login screen. The AI chat state is reset on sign-out.
  - Every request has a timeout (20 s; the AI chat uses 90 s). Timeouts, offline errors and ProblemDetails bodies become readable messages; 403 becomes "You do not have permission to do that."
- **Retest result:** 2026-10-01:
  - **Offline suite:** `flutter test` passes 65/65 (`testing/execution-results/phase7-flutter/final-test.log`). It includes these tests:
    - `auth_test.dart`:
      - "$role is refused and nothing is stored", run for HOTEL_OWNER, TRAVEL_AGENT and ADMIN
      - "a stored staff session is discarded on restore"
      - "a 401 after sign-in signs the user out, clears storage and explains why"
      - "an expired stored token on start-up explains why the user must sign in"
    - `api_service_test.dart`:
      - "reports a 401 on an authenticated request as an expired session"
      - "does not treat a failed sign-in or an anonymous 401 as an expired session"
      - "times out slow requests with a readable message"
      - "turns connection failures into an offline message"
  - **Live API:** `flutter test test_live` against the running API passes 9/9 (`final-live-api-test.log`):
    - "a hotel owner is refused by the mobile app and nothing is stored"
    - "a traveler signs in and is told when the token stops working"
    - "a traveler calling an admin API gets the permission message"
  - **Manual (Flutter web build in Chrome; the Android emulator stayed offline):** signing in as `owner@traveladvisor.com` shows the travelers-only message and stays on the login screen (`screenshots/01-owner-sign-in-refused.png`).

## DEF-015 Build outputs and logs tracked in git
- **Severity / Priority:** Low / P2
- **Description:** 265 `bin/`/`obj/` files, `android/.dart_tool`, `flutter_01.log`, and an empty `Untitled` file are tracked; there is no root `.gitignore`.
- **Steps to reproduce:** `git ls-files | Select-String "/(bin|obj)/"`.
- **Expected:** No build outputs tracked.
- **Actual:** 269 generated files tracked.
- **Evidence:** `git ls-files` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** Added a repository-root `.gitignore` and extended the project `.gitignore` (Flutter `.dart_tool` anywhere, logs, React build output) in `f07fcb1`. That commit did **not** actually remove the `bin/`/`obj/` files from the index (it committed modified copies of 43 of them). The 265 remaining build files were untracked with `git rm --cached` in `5fe2b9e`.
- **Retest result:** 2026-10-01 (first retest, after `f07fcb1`) - recorded as passing, but this was wrong: `git status` still listed tracked `bin/`/`obj/` files. 2026-10-01 (second retest, after `5fe2b9e`) - `git ls-files | Select-String "/(bin|obj)/"` returns 0 entries. Pass.

## DEF-016 API tests share a persistent database
- **Severity / Priority:** Low / P2
- **Description:** `ApiFixture` reuses `travel_advisor_test` on the local server without resetting it, so reruns can fail (e.g. booking overlap on the same dates).
- **Steps to reproduce:** Run `dotnet test` twice on the same day.
- **Expected:** Independent, repeatable runs.
- **Actual:** State leaks between runs.
- **Evidence:** `tests/TravelAdvisor.Api.Tests/ApiFixture.cs` at `4833828`.
- **Status:** Retested (Closed)
- **Fix:** `ApiFixture` now starts a fresh `postgres:16-alpine` Testcontainer per run (random password). Fallback: a uniquely named database on a local server (password from `TEST_PG_PASSWORD`), dropped on dispose.
- **Retest result:** 2026-10-01 - consecutive `dotnet test` runs pass (8/8 API tests) against fresh containers.

---

Defects below were found while implementing and executing tests.

## DEF-017 Domain-model migration fails on databases that already contain itineraries or bookings
- **Severity / Priority:** High / P1
- **Description:** The generated `AddDomainModelV2` migration added the NOT NULL columns `Itineraries.Travelers` and `Bookings.Guests` with a default of 0. Existing rows were filled with 0, which violates the new check constraints `CK_Itineraries_Travelers` / `CK_Bookings_Guests` (`> 0`). Empty databases (the API test containers) were not affected, so the test suite passed.
- **Steps to reproduce:** On a database migrated to `AddHotelAndPackageImages` that has at least one itinerary, run `dotnet ef database update`.
- **Expected:** Migration applies.
- **Actual:** `23514: check constraint "CK_Itineraries_Travelers" of relation "Itineraries" is violated by some row`; the transaction rolls back.
- **Evidence:** `dotnet ef database update` output against the local development database, 2026-10-01.
- **Status:** Retested (Closed)
- **Fix:** The migration now backfills both columns with 1. New audit timestamp columns were also changed from `0001-01-01` to `DEFAULT now()`.
- **Retest result:** 2026-10-01 - `dotnet ef database update` on the same development database completes ("Done."); API tests pass on a fresh container.

## DEF-018 Flutter lists do not refresh after cancel, review or Retry in debug builds
- **Severity / Priority:** Medium / P2
- **Description:** After cancelling a booking, the snackbar says "Booking cancelled" but the card still shows Pending with its Cancel button until the screen is reopened. The cause is `setState(() => _future = _load())`: the arrow callback returns the new `Future`, so Flutter's debug assertion in `setState` throws before the widget is marked for rebuild. In the booking list the throw was swallowed by the surrounding `catch`. The same pattern sat behind Retry and pull-to-refresh in the booking, itinerary, conversation, destination, hotel, package, preferences and reviews widgets. Release builds skip the assertion, so it only shows in debug and test builds, which is where it was found.
- **Steps to reproduce:** In a debug build, create a room booking, then on My Bookings tap Cancel booking and confirm.
- **Expected:** The card changes to Cancelled.
- **Actual:** The card still shows Pending; the API has already cancelled it (`GET /api/bookings/15` returns `status: 2`).
- **Evidence:** Phase 7 walkthrough, `testing/execution-results/phase7-flutter/screenshots/08-defect-stale-status-after-cancel.png`. The new widget test "cancelling refreshes the list with the status from the API" failed before the fix: the PATCH and the follow-up GET were both sent, but the list still rendered "Pending".
- **Status:** Retested (Closed)
- **Fix:** All 13 call sites assign the future inside a block body (`setState(() { _future = _load(); })`, via a `_reload()` helper where a screen used it several times).
- **Retest result:** 2026-10-01:
  - The widget test passes, as do the other 64 tests (`final-test.log`).
  - Manual: in the rebuilt web app, booking #16 was created and then cancelled, and the card changed to Cancelled in place (`screenshots/11-cancel-refreshes-after-fix.png`).

## DEF-019 AI planner treats the origin city as the destination
- **Severity / Priority:** High / P1
- **Description:** `RequirementParser.FindDestination` checked catalog names longest first and ignored context. When a message named both an origin and a destination, the longer name won. "Plan 3 days in Ella … from Kandy" was planned for Kandy (5 letters beats Ella's 4), and "to Ella from Nuwara Eliya" would have been planned for Nuwara Eliya.
- **Steps to reproduce:** As a USER, `POST /api/ai/chat` with "Plan 3 days in Ella for 2 people from Kandy by train, budget Rs. 60,000".
- **Expected:** A plan for Ella, with Kandy as the origin for transport.
- **Actual:** A plan for Kandy. In the mocked AI evaluation (Kandy has no hotels there) case A-05 returned `no_match`.
- **Evidence:** `testing/execution-results/phase8-tests/ai-evaluation/fixes-before-after.txt`. With the fix stashed, `Def019_origin_city_is_not_planned_as_the_destination` against the seeded PostgreSQL catalog failed with `Expected: "Ella"`, `Actual: "Kandy"`.
- **Status:** Retested (Closed)
- **Fix:** A catalog name that directly follows "from" (optionally "from the") is skipped when choosing the destination. If the only catalog name is the origin, the destination stays missing and the assistant asks for it.
- **Retest result:** 2026-10-01. The API test passes. The parser theory `Def019_origin_city_is_not_taken_as_the_destination` (4 cases) and `Def019_only_an_origin_leaves_the_destination_missing` pass. AI evaluation A-05 passes and selects the Kandy to Ella train.

## DEF-020 Model replies can quote hotels and prices that no tool returned
- **Severity / Priority:** Medium / P2
- **Description:** On the LLM path, when the model answered without calling a tool, its text went to the user as-is. Only secrets, system-prompt leaks and false booking claims were filtered. A reply such as "The Grand Ella Palace has rooms for Rs. 5,000 a night" (a hotel and price that do not exist in the catalog) reached the user.
- **Steps to reproduce:** In the AI evaluation, case J-06 scripts the model to answer "What hotels are in Ella?" with that sentence and no tool call.
- **Expected:** Prices shown to the user come from catalog tools.
- **Actual:** The invented hotel and price were returned unchanged (`J-06 failed: invented hotel shown to the user; invented price shown to the user`).
- **Evidence:** `testing/execution-results/phase8-tests/ai-evaluation/fixes-before-after.txt`
- **Status:** Retested (Closed), with a limitation
- **Fix:** `AgentOrchestrator` checks LLM replies that end without a plan or proposal. If the reply quotes an amount (Rs., LKR, rupees, USD, $), no catalog tool succeeded in that turn, and the amount does not appear in an earlier assistant message, the reply is replaced with a prompt to look the information up, and a warning is logged.
- **Retest result:** 2026-10-01. J-06 passes. J-08 also passes: a follow-up that repeats a price from an earlier tool-backed turn is kept, so the check does not block normal conversation. All 71 evaluation cases pass.
- **Limitation:** The check covers prices, not names. An invented hotel name mentioned without any price is not detected. The system prompt still instructs the model to use tools for every fact.

## DEF-021 React sign-in ignores the page the visitor asked for, and keeps the previous user's page after Sign out
- **Severity / Priority:** Low / P3
- **Description:** `RequireRole` sends an anonymous visitor to `/login` with the requested page in `state.from`, and `LoginPage` was meant to return there after sign-in. It never did: `login()` sets the user, `LoginPage` re-renders into `<Navigate to={home}>`, and that redirect overrides the `navigate(from)` in `submit()`. Once that was fixed, a second problem showed. Pressing Sign out also goes through `RequireRole`, which stored the page being left as `from`, so the next person to sign in was sent to the previous user's page. For a different role that page shows 403. For the same role, they land on someone else's last page instead of their home page.
- **Steps to reproduce:** (1) Signed out, open `/owner/hotels` and sign in as the hotel owner: you land on `/owner`. (2) After the first fix: as the travel agent on `/agent/packages`, press Sign out and sign in as the admin: you land on `/forbidden`.
- **Expected:** (1) Return to `/owner/hotels`. (2) Go to the admin home page.
- **Actual:** (1) `/owner`. (2) `/forbidden`; for a second hotel owner, `/owner/hotels` instead of `/owner`.
- **Found by:** Playwright against the real API (`e2e/auth.spec.ts`, `e2e/listings.spec.ts`), then reproduced in Vitest.
- **Evidence:** `testing/execution-results/phase8-tests/def021-before-fix.log` (deep link lost: received `/owner`), `def021b-before-fix.log` (admin after agent sign-out: received `/forbidden`), `def021c-before-fix.log` (second owner: received `/owner/hotels`), `def021-after-fix.log` (193 passed).
- **Status:** Retested (Closed)
- **Fix:** `LoginPage` uses the same target in both places through `postLoginPath(role, from)`, which returns `from` only if that role may open it and otherwise the role's home page. `AuthProvider` records an explicit Sign out (`signedOut`), and `RequireRole` then redirects to `/login` without a `from`. Reordering `logout()` and `navigate()` in the Sign out handler was tried first and did not work, because React Router runs navigation in a transition.
- **Retest result:** 2026-10-01. The three regression tests in `LoginPage.test.tsx` and `App.test.tsx` pass, as do 76 `postLoginPath` checks generated from the App route table. Playwright: 16 of 16 passed, twice in a row.

## DEF-022 Flutter cards overflow when text is wider than the space
- **Severity / Priority:** Low / P3
- **Description:** Four widgets laid text out in fixed `Row`s or fixed-height `Column`s with no way to wrap or shorten:
  - `HotelCard`: rooms, rating, `Spacer` and price in one Row.
  - `PackageCard`: duration and rating in one Row.
  - `DestinationCard`: a fixed 120 px image above the name and country, inside a 200 px tall slot.
  - `ReviewsSection`: the header, with the title, stars and "4.6 / 5 (1280 reviews)" in one Row.
  When the text needs more room than the card has, Flutter reports a RenderFlex overflow and draws the yellow-and-black stripe over the clipped content.
- **Found by:** The new mocktail widget tests. The `flutter_test` font draws every glyph 1 em wide, about twice as wide as Roboto, so the hotel and reviews rows overflowed on the default test screen. Narrow screens, long names and a larger system font size would produce the same overflow in the real app; that conclusion comes from the layout code and was not observed on a device.
- **Steps to reproduce:** `test/layout_test.dart`: a 320 x 640 screen with a 1.3 text scale, rendering a hotel with 124 rooms, rating 4.7 (1280 reviews) and LKR 125,000; a package with a long duration; the Home destination card for "Nuwara Eliya Highlands", "United Arab Emirates"; and a reviews header for 1280 reviews.
- **Expected:** The cards lay out without overflow.
- **Actual:** "A RenderFlex overflowed by" 176 px (destination card, bottom), 444 px (hotel card, right), 112 px (package card, right) and 206 px (reviews header, right).
- **Evidence:** `testing/execution-results/phase8-tests/def022-before-fix.log` (4 failed); `flutter-test.log` after the fix.
- **Status:** Retested (Closed)
- **Fix:** The hotel card and reviews header use a `Wrap`, so the price or the rating summary moves to the next line when needed. The hotel and package details are a single `Text.rich` (shared `iconLabel` helper in `lib/widgets/icon_label.dart`), so they wrap like ordinary text. The destination image is `Expanded` and gives up height to the text, and the name and country are one line with an ellipsis.
- **Retest result:** 2026-10-01. The four layout tests pass, and the full suite passes (94 tests, `flutter-test.log`). `flutter analyze`: no issues.

## DEF-023 Flutter "Itinerary saved" notice never closes and covers the chat's Send button
- **Severity / Priority:** Medium / P2
- **Description:** After a traveler saves an AI plan, the chat shows a snack bar "Itinerary saved" with a View action. In the Flutter SDK in use (3.47.5), a snack bar with an action defaults to `persist: true` (`snack_bar.dart`: `persist = persist ?? action != null`) and stays until the action is used or it is swiped away. The snack bar sits over the message box and Send button, so the traveler cannot send the next message. Tapping where Send is shown hits View instead and opens the saved itinerary, leaving the chat.
- **Found by:** The Flutter integration test against the real API (`integration_test/traveler_journey_test.dart`). After saving, its tap on Send opened the itinerary page ("On screen: Ella trip | … Status: Draft"). Waiting for the notice to close then timed out after 15 seconds.
- **Steps to reproduce:** Sign in as a traveler, ask the assistant to plan a trip, tap Save itinerary, then wait.
- **Expected:** The notice closes after a few seconds and the next message can be sent.
- **Actual:** The notice is still on screen after 10 seconds (widget test) and 15 seconds (browser), covering the message box.
- **Evidence:** `testing/execution-results/phase8-tests/def023-before-fix.log` (regression test failed: `Found 1 widget with text "Itinerary saved"`, expected none), `def023-after-fix.log` (passed); integration runs 7 (failed) and 8 (passed): `flutter-integration-run7-profile.log`, `flutter-integration.log`, `flutter-integration-console.log`.
- **Status:** Retested (Closed)
- **Fix:** The snack bar sets `persist: false`, so it closes after the default 4 seconds. The View action is still available while it is shown.
- **Retest result:** 2026-10-01. The regression test "the "Itinerary saved" notice closes by itself and does not block the message box (DEF-023)" passes, and so does the integration journey (3 of 3).

## DEF-024 Saved itineraries lose the number of travelers, the budget and the source conversation
- **Severity / Priority:** Medium / P2
- **Description:** Phase 2 added `Travelers`, `Budget` and `ConversationId` to the `Itineraries` table, but nothing ever set them. `CreateItineraryRequest` had no such fields, `ItineraryService` did not map them, `ItineraryDto` did not return them, and neither client sent them. Every saved plan was stored with `Travelers = 1`, no budget and no link to the AI conversation that produced it. A 2-person plan therefore became a 1-person itinerary in the database; only the free-text summary still said "2 travelers".
- **Found by:** The full E2E journey, which checks the database directly (`web-react/e2e/ai-journey.spec.ts`). After saving a 2-person Ella plan from the React assistant, `SELECT "Travelers" FROM "Itineraries"` returned 1.
- **Steps to reproduce:** As a traveler, ask the assistant for "a 3-day trip to Ella … for 2 people with a budget of LKR 50000", press Save as itinerary, and read the row in `Itineraries`.
- **Expected:** `Travelers = 2`, `Budget = 50000`, `ConversationId` = the chat's id.
- **Actual:** `Travelers = 1`, `Budget` and `ConversationId` NULL.
- **Evidence:** `testing/execution-results/phase8-tests/e2e-journey-run1.log` (`Expected: 2, Received: 1`); `def024-before-fix.log` (new API test failed: no `travelers` in the response); `def024-after-fix.log`, `e2e-journey-run2.log` (passed).
- **Status:** Retested (Closed)
- **Fix:**
  - `CreateItineraryRequest` accepts optional `travelers` (1-50, default 1), `budget` (≥ 0) and `conversationId`, and `ItineraryDto` returns all three.
  - A conversation id must belong to the caller. Someone else's id and a missing one both get the same 400 "Conversation not found.", so ids cannot be probed.
  - The React assistant (`planToItinerary`) and the Flutter app (`toItineraryRequest`, `ChatProvider.saveLatestPlan`) send the plan's travelers and budget and the current conversation id.
- **Retest result:** 2026-10-01.
  - API test `Def024_saved_plan_keeps_travelers_budget_and_its_own_conversation` passes: values stored and returned, default of 1, travelers 0 rejected, a stranger's or unknown conversation rejected. Full suites: unit 397 passed / 35 skipped (live LLM), API 129 passed (`backend-unit-tests-def024.log`, `backend-api-tests-def024.log`).
  - Vitest 286 passed and Flutter 94 passed, with the request-mapping tests extended.
  - The E2E journey passes with the database checks for travelers, budget and conversation. The Flutter integration test (run 9) also checks `travelers` and `conversationId` through the API and passes.

## DEF-025 Saved itineraries reorder the items within a day
- **Severity / Priority:** Medium / P2
- **Description:** The web and mobile apps number itinerary items from 0 within each day. `ItineraryService.CreateAsync` stored `SortOrder = item.SortOrder != 0 ? item.SortOrder : index`, where `index` is the position in the whole request. On every day after the first, the first item (sort order 0) was replaced by a larger number than its neighbours, so it moved to the end of its day. The itinerary page then showed, for example, "Check out of the hotel" before "Explore Ella at your own pace" on the last day.
- **Found by:** Reading the database rows written by the passing E2E journey. Day 3 of the plan stored in the conversation was "Explore Ella", then "Check out", but the saved items had sort orders 3 and 1. That first dump was overwritten by the after-fix dump (`e2e-journey-db-rows.txt`); `def025-before-fix.log` reproduces the defect.
- **Steps to reproduce:** `POST /api/itineraries` with day 3 items "Explore Ella" (sortOrder 0) and "Check out" (sortOrder 1), then `GET /api/itineraries/{id}`.
- **Expected:** Explore Ella, Check out.
- **Actual:** Check out, Explore Ella.
- **Evidence:** `testing/execution-results/phase8-tests/def025-before-fix.log` (`Expected: [..., "Explore Ella", "Check out"]`, `Actual: [..., "Check out", "Explore Ella"]`), `def025-after-fix.log`.
- **Status:** Retested (Closed)
- **Fix:** Items are ordered by day, then the given sort order, then request order. They are then numbered 0, 1, 2… in that order, so explicit sort orders are respected, ties keep the request order, and nothing moves.
- **Retest result:** 2026-10-01.
  - `Def025_saved_itinerary_keeps_the_order_of_items_within_each_day` passes: per-day numbering, no sort orders, and explicit reordering.
  - Full suites: unit 397 passed / 35 skipped, API 130 passed.
  - The E2E journey now compares the saved items, ordered by day and sort order, with the plan stored in the conversation, and passes (`e2e-full-suite.log`).
