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
- **Retest result:** 2026-10-01 - `git grep -e vipoo -e SuperSecretKeyForJwt` over tracked files returns no matches; `dotnet test` passes (19 unit, 8 API).

## DEF-002 Concurrent room bookings can double-book the same dates
- **Severity / Priority:** High / P1
- **Description:** `BookingHelper.ValidateAndPriceAsync` checks for overlap, then the controller inserts in a separate step with no transaction, lock, or database constraint.
- **Steps to reproduce:** Send two simultaneous `POST /api/bookings` requests for the same room and dates.
- **Expected:** Exactly one succeeds; the other gets 409 Conflict.
- **Actual:** Both can succeed.
- **Evidence:** `shared/Infrastructure/Helpers/BookingHelper.cs` lines 43-50 at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-003 Deactivated users keep working JWTs
- **Severity / Priority:** High / P1
- **Description:** Token validation does not check `IsActive`; a deactivated user keeps access for up to 24 hours.
- **Steps to reproduce:** Log in as an owner, have Admin deactivate the account, call `GET /api/hotels/mine` with the old token.
- **Expected:** 401 Unauthorized.
- **Actual:** 200 OK.
- **Evidence:** `shared/Infrastructure/DependencyInjection.cs` JWT setup has no `OnTokenValidated` hook.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-004 Login brute force is possible
- **Severity / Priority:** High / P2
- **Description:** `CheckPasswordSignInAsync(user, password, false)` disables lockout and there is no rate limiting.
- **Steps to reproduce:** Send repeated wrong-password logins for one account.
- **Expected:** Account locks or requests are throttled.
- **Actual:** Unlimited attempts accepted.
- **Evidence:** `web-api/Controllers/AuthController.cs` line 81 at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-005 Login email comparison is case-sensitive
- **Severity / Priority:** Medium / P2
- **Description:** Login uses `u.Email == request.Email`, which PostgreSQL compares case-sensitively.
- **Steps to reproduce:** Log in as `ADMIN@TravelAdvisor.com` with the correct password.
- **Expected:** 200 OK with JWT.
- **Actual:** 401 Unauthorized.
- **Evidence:** `web-api/Controllers/AuthController.cs` line 74 at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-006 Booking dates and duplicates are not validated
- **Severity / Priority:** Medium / P2
- **Description:** Past check-in dates are accepted, a missing check-in becomes `DateTime.MinValue`, room capacity is ignored, and identical package bookings can be created repeatedly.
- **Steps to reproduce:** `POST /api/bookings` with `checkIn` = yesterday, or the same package and date twice.
- **Expected:** 400 Bad Request / 409 Conflict.
- **Actual:** 201 Created.
- **Evidence:** `BookingHelper.cs` at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

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
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-011 Admin bypass in provider update endpoints is unreachable
- **Severity / Priority:** Low / P3
- **Description:** Update actions check `!IsAdmin()` but the endpoint policy only admits HOTEL_OWNER / TRAVEL_AGENT, so admins can never edit listings.
- **Steps to reproduce:** Admin calls `PUT /api/hotels/{id}`.
- **Expected:** Admin moderation allowed (or the dead code removed).
- **Actual:** 403 Forbidden.
- **Evidence:** `HotelsController.cs` line 137 at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

## DEF-012 Swagger, migrations and demo seeding run in Production
- **Severity / Priority:** Low / P2
- **Description:** Swagger is enabled in every non-Testing environment and demo users with known passwords are always seeded.
- **Steps to reproduce:** `docker compose up`, browse `/swagger`, log in as `admin@traveladvisor.com / Admin@123`.
- **Expected:** Configurable; disabled by default in Production.
- **Actual:** Always on.
- **Evidence:** `web-api/Program.cs` lines 25-38 at `4833828`.
- **Status:** Open
- **Fix:**
- **Retest result:**

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
- **Fix:** `git rm --cached` on 269 generated files; added a repository-root `.gitignore` and extended the project `.gitignore` (Flutter `.dart_tool` anywhere, logs, React build output).
- **Retest result:** 2026-10-01 - `git ls-files | Select-String "/(bin|obj)/"` returns 0 entries.

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
