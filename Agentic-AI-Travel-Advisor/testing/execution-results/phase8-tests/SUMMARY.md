# Phase 8: Test suites against the real system

Run date: 2026-10-01, Windows 11, .NET SDK 8.0.423, Docker Desktop 29.2.0.
Every number below comes from a run whose raw output is stored in this folder.

## 8a. Backend: xUnit + Moq service tests, WebApplicationFactory API tests

### What was added

| File | Kind | What it checks |
|---|---|---|
| `tests/TravelAdvisor.UnitTests/Services/BookingServiceTests.cs` | Moq | Room and package booking rules (date window, max nights, settings-driven advance window, capacity, blocked nights, overlap, seats left, duplicates), price overrides, availability quotes, role-scoped listing, access checks, status transitions, cancellation cutoff, payment refund/void on cancel. Every rejection also verifies that nothing was added or saved. |
| `tests/TravelAdvisor.UnitTests/Services/AuthServiceTests.cs` | Moq (`UserManager`, `SignInManager`, JWT service) | Registration (trimmed fields, USER role, duplicate email 409, Identity errors mapped to `password`/`email`, missing role seed), login (lockout on failure, identical 401 for unknown email and wrong password, deactivated user never reaches the password check, lockout message), profile update. |
| `tests/TravelAdvisor.UnitTests/Services/UserServiceTests.cs` | Moq | Staff creation limited to HOTEL_OWNER/TRAVEL_AGENT, duplicate email, self-deactivation refused, security-stamp rotation and status-cache invalidation on deactivation, profile/preference normalisation. |
| `tests/TravelAdvisor.UnitTests/Services/CatalogServiceTests.cs` | Moq | Hotel/room/package/destination services: approved-only public search, visibility of pending listings, provider-versus-provider 403s, re-approval after provider edits (not after admin edits or no-op saves), delete blocked by bookings, calendar rules, activity day range, destination uniqueness. |
| `tests/TravelAdvisor.Api.Tests/AccessControlMatrixTests.cs` | WebApplicationFactory + Testcontainers | 57 protected endpoints x 5 callers (anonymous, USER, HOTEL_OWNER, TRAVEL_AGENT, ADMIN); 9 public endpoints; malformed `Authorization` headers. |
| `tests/TravelAdvisor.Api.Tests/OwnershipAndContractTests.cs` | WebApplicationFactory + Testcontainers | Duplicate email (case and whitespace variants), role supplied at registration ignored, password policy boundaries, identical 401 messages, 404 ProblemDetails for every resource type, agent-vs-agent and owner-vs-owner IDOR, agent cannot manage a hotel booking and vice versa, mass assignment on bookings and hotels, itineraries/conversations private even from admins. |

### Results

| Suite | Command | Result | Evidence |
|---|---|---|---|
| Unit (incl. 114 new Moq tests) | `dotnet test tests/TravelAdvisor.UnitTests` | **320 passed, 0 failed, 0 skipped** | `backend-unit-tests.log`, `backend-unit-tests.trx` |
| API integration (incl. 19 new) | `dotnet test tests/TravelAdvisor.Api.Tests` | **73 passed, 0 failed, 0 skipped** (fresh `postgres:16-alpine` container per run) | `backend-api-tests.trx`, `backend-api-tests-verbose.log` |
| Access-control matrix | part of the API run | 285 checks, 0 failures | `rbac-matrix.txt` |

### Checking that the Moq tests catch regressions

To confirm the new tests are not vacuous, two rules were broken on purpose in the source and the service tests were rerun, then the source was restored (`git diff` on `shared/Infrastructure/Services` was empty afterwards):

- Disabling the room-overlap check in `BookingService` failed `Overlapping_booking_is_a_conflict_and_nothing_is_saved` and `Availability_reports_rule_failures_instead_of_throwing`.
- Making `Ownership.StatusAfterEdit` always keep the current status failed `Owner_edit_sends_an_approved_hotel_back_for_review`, `Price_change_sends_the_package_back_for_review` and `Adding_an_activity_requires_re_approval`.

Result of the mutated run: 5 failed, 109 passed. This run was not kept as a file, because it was a throwaway check against deliberately broken code.

### Findings while writing the tests

- **First matrix run: 18 mismatches, not a defect.** `GET /api/ai/conversations`, `GET /api/ai/conversations/{id}`, `GET /api/ai/recommendations` and `GET/DELETE /api/itineraries[/{id}]` accept any signed-in role. Only the `POST` endpoints are limited to USER. `documentation/API.md` lists these reads as "Authenticated", and the services always filter by the caller's own user id, even for admins. The matrix was corrected to the documented contract, and `Itineraries_and_conversations_are_private_even_from_admins` proves that another traveler, an owner, an agent and an admin get 404 or an empty list for a traveler's itinerary, conversation and recommendations.
- Two test mistakes were corrected rather than the API:
  - `POST /api/packages/{id}/activities` returns `200 OK`, not `201`. That is the existing contract.
  - After an agent adds an activity the package goes back to Pending, so an anonymous read returns 404. That is the re-approval rule (DEF-010) working as designed.
