# Test Plan

## Scope

This plan covers the existing Agentic AI Travel Advisor application: the ASP.NET Core API and PostgreSQL-backed services, React web app, MVC portal, Flutter traveler app, and their principal user journeys. The repository already contains automated suites; this run does not add or change tests or application code.

## Features in scope

- User registration, authentication, session access, and role-based authorization.
- Public destination, hotel, package, and transportation discovery.
- Provider listing management and admin approval.
- AI travel planning, recommendations, itinerary creation, and booking proposals.
- Availability quotes, booking creation, status transitions, persistence, and conflict handling.
- Traveler-facing React and Flutter journeys.

These are planned coverage areas. No application feature was successfully exercised in this run.

## Test environment

| Item | Value |
|---|---|
| Operating system | Windows (workspace environment) |
| Repository | `vipooshans/Agentic-AI-Travel-Advisor` |
| Shell access | Unavailable. The PowerShell launcher could not start because neither `pwsh.exe` nor `powershell.exe` was found. |
| .NET | Project targets .NET 8; installed SDK/runtime version not verified. |
| Node.js / npm | `web-react/package.json` requires Node.js `^22.12.0 || >=24.0.0`; installed versions not verified. |
| Flutter / Dart | README requires Flutter 3; installed versions not verified. |
| Database / container runtime | PostgreSQL 16 and Docker are listed as requirements for API integration tests; availability not verified. |
| Application | Could not be started in this session. Attempts to open `localhost:5173`, `localhost:5080/swagger`, and `localhost:7000` failed to connect. |
| Test run date | 2026-10-06 |

Runtime versions and Docker availability are **not verified**; no version or availability is inferred from the project requirements.

## Existing automated test assets

The repository contains:

- .NET xUnit projects: `tests/TravelAdvisor.UnitTests` and `tests/TravelAdvisor.Api.Tests`.
- React/Vitest tests in `web-react/src/` and an E2E command in `web-react/package.json`.
- Flutter unit/widget tests in `mobile-app/test/` and an integration test in `mobile-app/integration_test/`.
- Postman/Newman API contract tests under `testing/integration/postman/`.

The README documents the canonical commands, including `dotnet test`, `npm test`, `npm run e2e`, and `flutter test`. They were not run in this session because a shell could not be launched. The built-in test runner was tried for the React and Flutter unit/widget test files, but returned “No tests found in the files”; it did not produce test counts or run those tests.

## Test cases

| Test ID | Feature / journey | Input and steps | Expected result |
|---|---|---|---|
| TC-001 | .NET unit suite | Run `dotnet test tests/TravelAdvisor.UnitTests`. | Test runner completes; report actual passed, failed, and skipped counts. Optional live-LLM evaluations may skip without their configured API key. |
| TC-002 | API, database, and security suite | Run `dotnet test tests/TravelAdvisor.Api.Tests` with the documented Docker/PostgreSQL prerequisites. | API integration tests complete against the configured test database; report actual counts. |
| TC-003 | React automated suite | From `web-react`, run `npm test`. | Vitest completes and reports all test counts and exit status. |
| TC-004 | Flutter analysis and unit/widget suite | From `mobile-app`, run `flutter analyze` and `flutter test`. | Analysis and tests complete without unexpected diagnostics or failures; report each command separately. |
| TC-005 | API startup and health | Start the API using the documented command and request `GET /api/health`. | API starts with valid database/JWT configuration and health endpoint returns success. |
| TC-006 | React app startup and rendered UI | Start the API and React dev server; open `http://localhost:5173`. | Home page renders and its public content is visible; client API calls use the documented proxy target. |
| TC-007 | Valid registration and persistence | Submit a unique valid email, password meeting the documented complexity rules, first name, and last name; then sign in and retrieve the current user. | Registration succeeds as a `USER`; authentication succeeds and the account is retrievable. |
| TC-008 | Invalid registration input | Submit empty required values, malformed email, and a password that does not meet the documented policy. | Input is rejected with a validation response; no account is created. |
| TC-009 | Authentication failures | Submit an unknown email/wrong password; then request `GET /api/auth/me` without a bearer token. | Login returns the documented generic 401 response; the protected request returns 401. |
| TC-010 | Public catalog search and edge inputs | Request destination, hotel, and package listings with a valid search term, an empty query, and a malformed filter value. | Public requests return only publicly visible/approved listings; empty and invalid filters are handled without server errors. |
| TC-011 | Role restrictions and ownership | Call a protected provider/admin operation without a token, with a role lacking permission, and with a resource owned by another provider. | Requests are rejected with the documented 401/403 or resource-hiding 404 behavior; no unauthorized change is persisted. |
| TC-012 | AI travel assistant | As a traveler, submit a trip-planning request (for example, three days in Galle for two travelers with an LKR 60,000 budget); repeat with an empty prompt and without authentication. | Valid input returns a structured plan or a clear configured-service response; empty and unauthenticated requests are rejected as documented. |
| TC-013 | Availability and booking lifecycle | Quote an available room for future dates, create a booking, read it back, and exercise a conflicting overlapping booking. | Quote and booking use server-calculated price; created booking persists as Pending; overlap is rejected with a conflict response. |
| TC-014 | Booking authorization and invalid dates | Submit a booking with check-out on/before check-in, guests above room capacity, or no/too many listing selectors; attempt an unauthorized status change. | Invalid business data is rejected; unauthorized status transition is denied; no invalid booking is persisted. |
| TC-015 | Flutter traveler journey | Run the documented Flutter integration journey against the running API in Chrome: register/sign in, book a room, and plan/save/confirm a trip. | Traveler journey completes and the resulting booking/trip state is visible through the app/API. |

## Execution policy

- Record only command output and behavior observed in this test run.
- Treat tests that could not be launched or whose prerequisites were unavailable as **Not run**, not as passing.
- Do not modify application code or existing test files as part of this run.
