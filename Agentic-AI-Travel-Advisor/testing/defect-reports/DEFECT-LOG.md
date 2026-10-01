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
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-008 Budget parser misses budgets and mislabels currency
- **Severity / Priority:** Medium / P2
- **Description:** The first regex match for a keyword is rejected if small (e.g. "max 4 people") and later matches are never tried; USD/$ amounts are treated as LKR; "travellers" is not recognised.
- **Steps to reproduce:** "Trip to Ella, max 4 people, budget Rs. 60000".
- **Expected:** budget = 60000, travelers = 4.
- **Actual:** budget = null (assistant asks for a budget).
- **Evidence:** `TravelPlanningAgent.ParseBudget` at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-009 MVC portal returns 500 on expired token or API 404
- **Severity / Priority:** Medium / P2
- **Description:** `GetFromJsonAsync` throws on non-success status codes; no 401 handling redirects to login.
- **Steps to reproduce:** Let the API token expire (or restart the API with a new key) and open `/Owner/Hotels`.
- **Expected:** Redirect to login.
- **Actual:** HTTP 500 error page.
- **Evidence:** `web-app/Services/TravelApiClient.cs` at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

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
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-014 Flutter app: wrong-role login, no 401 handling, missing timeouts
- **Severity / Priority:** Low / P2
- **Description:** Owner/agent/admin accounts can log in to the traveler app and then hit 403s; expired tokens do not log out; only the AI call has a timeout.
- **Steps to reproduce:** Log in to the app as `owner@traveladvisor.com`, open Bookings.
- **Expected:** Clear message that the app is for travelers.
- **Actual:** "Failed to load bookings".
- **Evidence:** `mobile-app/lib/providers/auth_provider.dart`, `services/api_service.dart` at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

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
