# Phase 1 - Backend architecture and security hardening

- **Date:** 2026-10-01
- **Base commit:** `a2dbc1b` plus the uncommitted Phase 1 changes (committed right after this run)
- **Database:** Testcontainers `postgres:16-alpine` (fresh container per run)
- **Command:**
  - `dotnet test tests/TravelAdvisor.UnitTests --logger "trx;LogFileName=unit-tests.trx"`
  - `dotnet test tests/TravelAdvisor.Api.Tests --logger "trx;LogFileName=api-tests.trx"`

## Results

- Unit tests: 19 passed, 0 failed, 0 skipped (`unit-tests.trx`, `unit-tests.log`).
- API integration tests: 20 passed, 0 failed, 0 skipped (`api-tests.trx`, `api-tests.log`).
  - The 8 pre-existing tests still pass after controllers were moved onto the service layer, so the API contract is unchanged.
  - 12 new tests in `SecurityHardeningTests` cover DEF-003, DEF-004, DEF-005, DEF-011 and DEF-012, plus admin-API access by a normal user, cross-user booking access (IDOR), invalid, expired, unsigned and role-escalated JWTs, ProblemDetails validation responses, security headers and login rate limiting.

## Notes

- A manual smoke test of the running API (Development environment, local database) found that the security headers were missing from error responses, because the exception handler clears response headers. The headers are now added in a `Response.OnStarting` callback, and `Responses_carry_security_headers` checks a 404 response as well. The results above come from the run after that fix.
- Manual smoke test results (curl / PowerShell against `http://localhost:5080`):
  - `/api/health` returned 200 and the database was connected.
  - `/swagger/v1/swagger.json` returned 200 with the bearer scheme (Development only).
  - Login with an upper-case email worked.
  - `/api/reports/summary` returned admin totals.
  - `/api/hotels?q=ella&maxPrice=10000` returned one hotel with `minPricePerNight` 8000.
  - `/api/hotels/99999` returned 404 `application/problem+json` with `message`.

- `api-tests.log` contains `Request failed with 401/403/404` information lines from the API's exception handler. These are expected, because the tests deliberately send bad credentials and forbidden requests.
