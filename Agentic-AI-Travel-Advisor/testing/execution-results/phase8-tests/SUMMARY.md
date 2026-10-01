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

## 8b. Database: constraints, transactions, migrations, booking consistency

### What was added

`tests/TravelAdvisor.Api.Tests/DatabaseIntegrityTests.cs` runs against the same Testcontainers PostgreSQL as the API tests. Constraint checks are plain SQL inside a transaction that is always rolled back, so they prove what PostgreSQL itself enforces, not EF validation, and they leave no rows behind. Each rejection is asserted by SQLSTATE and by the exact constraint name.

| Area | Cases |
|---|---|
| Foreign keys (23503) | room to missing hotel; booking to missing user or room; payment to missing booking; restrict on deleting a destination with packages, a role held by users, a room with bookings, a booking with a payment |
| Unique (23505) | normalised email, destination name+country, room name per hotel, role name, setting key, payment reference, one calendar override per room night, one review per booking |
| NOT NULL (23502) | hotel name, destination country, booking user, payment currency |
| Check (23514) | booking room-xor-package, check-out after check-in, guests > 0, total >= 0; room capacity and price; package duration and travelers; activity day; review rating 1-5 and single target; payment amount > 0; price override >= 0; budget min <= max; itinerary dates |
| Exclusion (23P01) | overlapping or identical stays in the same room (`EX_Bookings_Room_NoOverlap`) |
| Accepted edge cases | back-to-back stays, overlap with a cancelled stay, overlap in different rooms, package booking without a room, one review per booking, budget min = max, same-day itinerary |
| Schema | all 19 domain tables exist; all 22 named check/exclusion constraints exist; 33 foreign keys |
| Delete behaviour | itinerary keeps living with `DestinationId` set to NULL; itinerary items and room calendar rows cascade |
| Transactions | `UnitOfWork.ExecuteInTransactionAsync` rolls back an already-saved write when the action throws and clears the change tracker |
| Concurrency | two contexts editing one booking: the second save throws `DbUpdateConcurrencyException` (xmin row version) and the first edit is kept |
| Audit | `CreatedAt` is stamped by the interceptor (client value ignored), never changes on update, `UpdatedAt` moves, values are UTC |
| Seeding | running `DbSeeder` a second time leaves roles/destinations/settings/demo users at 4/9/5/3 |
| Migrations | on a separate freshly created database: all 6 migrations apply in order, the model snapshot has no drift from the current model, every `Down()` runs back to an empty schema, and `Up()` rebuilds it including the exclusion constraint |
| Booking consistency | a booking made through the API is stored with the right user, room, dates, guests, Pending status and total (3 nights x 9,000 = 27,000); cancelling sets status 2 and `CancelledAt`; the same dates can then be booked again |

### Results

| Suite | Command | Result | Evidence |
|---|---|---|---|
| API integration incl. database | `dotnet test tests/TravelAdvisor.Api.Tests` | **127 passed, 0 failed, 0 skipped** (73 from 8a + 54 database tests) | `backend-api-tests.trx`, `backend-api-tests-verbose.log` (this run replaces the 8a files) |
| Database tests only | extracted from the TRX above | 54 passed, 0 failed; the actual PostgreSQL error for every rejected case | `database-tests.txt` |

### Checking that the database tests can fail

Two expectations were broken on purpose and the database tests rerun: one expected constraint name was changed to a wrong name, and the "same-day itinerary" accepted case was changed to end before it starts. Three tests failed: the wrong name (`Expected: "CK_Reviews_Wrong"`, `Actual: "CK_Reviews_Target"`), the itinerary case (`rejected: 23514 CK_Itineraries_Dates`), and the schema test, which takes its expected names from the same list. The file was then restored.

The first full run after restoring still showed those 3 failures (124 passed). The restored file had kept the backup's older timestamp, so the incremental build reused the mutated assembly. After touching the file and rerunning, the result was 127 passed. That run is the one stored here.

### Findings

No database defects were found. All constraint names and delete behaviours matched the EF configuration, and the migration history round-trips without model drift.
