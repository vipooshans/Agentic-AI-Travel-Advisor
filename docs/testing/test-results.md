# Test Results

**Run date:** 2026-10-06  
**Environment:** Windows workspace; command-line shell unavailable. See [test-plan.md](test-plan.md).

No automated project test completed. The existing React and Flutter test files were identified in the repository, but the test-runner tool returned “No tests found in the files” for both requested file sets. No test pass/fail counts or command exit codes were produced. Attempts to open local application URLs also could not reach a listening service. These constraints do not establish whether the application or tests pass when run in a configured developer environment.

| Test ID | Feature | Input | Expected Result | Actual Result | Status (Pass/Fail/Not run) |
|---|---|---|---|---|---|
| TC-001 | .NET unit suite | `dotnet test tests/TravelAdvisor.UnitTests` | Test suite runs and reports counts. | Not run; terminal command execution was unavailable. | Not run |
| TC-002 | API/database/security suite | `dotnet test tests/TravelAdvisor.Api.Tests` | Integration suite runs with its documented prerequisites and reports counts. | Not run; terminal command execution and Docker availability could not be verified. | Not run |
| TC-003 | React automated suite | `npm test` from `web-react` | Vitest suite runs and reports counts. | Not run. Built-in runner attempt returned “No tests found in the files”; no Vitest command ran. | Not run |
| TC-004 | Flutter analysis and unit/widget suite | `flutter analyze`, then `flutter test` from `mobile-app` | Analysis and Flutter suite complete and report results. | Not run. Built-in runner attempt returned “No tests found in the files”; no Flutter command ran. | Not run |
| TC-005 | API startup and health | Start API, then `GET /api/health` | API starts and health endpoint succeeds. | Not run; could not launch the API. | Not run |
| TC-006 | React startup and UI | Open `http://localhost:5173` after starting API and dev server. | Home page renders and public UI is usable. | App was not started. Browser navigation returned `ERR_FAILED (-2) loading 'http://localhost:5173/'`. | Not run |
| TC-007 | Valid registration and persistence | Unique valid traveler registration, sign-in, and current-user read-back. | New USER account is created and retrievable. | Not run; application/API was not available. | Not run |
| TC-008 | Invalid registration input | Empty required fields, malformed email, and weak password. | Validation rejects invalid data without creating an account. | Not run; application/API was not available. | Not run |
| TC-009 | Authentication failures | Wrong/unknown credentials and `/api/auth/me` without a token. | Generic 401 for login failure and 401 for missing credentials. | Not run; application/API was not available. | Not run |
| TC-010 | Public catalog search | Destination/hotel/package searches with a term, empty query, and invalid filter. | Public results follow visibility rules and invalid inputs do not cause server errors. | Not run; application/API was not available. | Not run |
| TC-011 | Roles and resource ownership | Missing token, insufficient role, and another provider's resource. | Documented 401/403/404 response; no unauthorized mutation. | Not run; application/API was not available. | Not run |
| TC-012 | AI assistant | Valid bounded trip request, empty prompt, and unauthenticated request. | Structured plan or clear service response; invalid/unauthenticated requests rejected. | Not run; application/API was not available. | Not run |
| TC-013 | Availability and booking lifecycle | Future-date quote, create/read booking, then overlapping booking. | Correct quote and persisted Pending booking; conflict for overlap. | Not run; application/API and database were not available. | Not run |
| TC-014 | Invalid booking and authorization | Invalid dates/capacity/listing selectors and unauthorized status transition. | Invalid booking rejected; unauthorized transition denied; no invalid data persisted. | Not run; application/API was not available. | Not run |
| TC-015 | Flutter traveler journey | Documented Chrome integration journey against API on port 5080. | Traveler can book and plan/save/confirm a trip. | Not run; Flutter command and API startup were unavailable. | Not run |

## Observed endpoint probes

The app was not started before the following browser navigation attempts. These are recorded as evidence of the local environment at probe time, not as feature-test failures:

- `http://localhost:5173/` — `ERR_FAILED (-2) loading 'http://localhost:5173/'`
- `http://localhost:5080/swagger` — `ERR_FAILED (-2) loading 'http://localhost:5080/swagger'`
- `http://localhost:7000/` — `ERR_CONNECTION_REFUSED (-102) loading 'http://localhost:7000/'`

## Counts

| Total planned cases | Passed | Failed | Not run |
|---:|---:|---:|---:|
| 15 | 0 | 0 | 15 |
