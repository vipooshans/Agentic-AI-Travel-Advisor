# Phase 3 - Booking integrity and re-approval

- **Date:** 2026-10-01
- **Base commit:** `ce8a9b0` plus the uncommitted Phase 3 changes (committed right after this run)
- **Database:** Testcontainers `postgres:16-alpine` (fresh container per run), Docker 29.2.0
- **Command:**
  - `dotnet test tests/TravelAdvisor.UnitTests --logger "trx;LogFileName=unit-tests.trx"`
  - `dotnet test tests/TravelAdvisor.Api.Tests --logger "trx;LogFileName=api-tests.trx" --logger "console;verbosity=normal"`
  - `dotnet test tests/TravelAdvisor.Api.Tests --filter "FullyQualifiedName~Concurrent"`, run 5 times

## Results

- Unit tests: 29 passed, 0 failed, 0 skipped (`unit-tests.trx`, `unit-tests.log`).
  - 10 new cases test `BookingStatusHelper.IsBeforeCancellationCutoff` and `CanComplete`.
- API integration tests, first run: 30 passed, 1 failed (`api-tests-run1-failed.trx`, `api-tests-run1-failed.log`).
  - The failure was in the new test `Provider_edits_send_approved_listings_back_for_review_but_admin_edits_do_not`. It expected `201 Created` from `POST /api/packages/{id}/activities`, but that endpoint has always returned `200 OK`.
  - The test assumption was wrong; the product behaviour was not. The assertion was corrected and the API contract left as it was.
- API integration tests, second run: 31 passed, 0 failed, 0 skipped (`api-tests.trx`, `api-tests.log`).
  - All 20 tests from Phase 1 still pass, including `Booking_lifecycle_and_illegal_status`.
  - 11 new tests are in `BookingRulesTests`:
    - Past check-in, more than 365 days ahead, more than 30 nights, over room capacity and missing check-in each return 400. A valid 2-guest stay is priced at nights × rate.
    - An overlapping stay returns 409. A back-to-back stay (check-in on the previous guest's check-out day) is accepted.
    - **DEF-002:** 8 simultaneous requests for the same room and dates give exactly 1 × 201 and 7 × 409.
    - **DEF-002:** a raw SQL insert of an overlapping booking is rejected by PostgreSQL with `23P01` (exclusion violation). The database enforces the rule even if the API is bypassed.
    - Room calendar: a blocked night returns 409, and a price override changes the quote and booking total. Blocking a night that is already booked returns 409. Booked nights are listed.
    - Room calendar access: another hotel owner gets 403 on GET/PUT, and a guest gets 403 on PUT.
    - **DEF-006:** package capacity (`MaxTravelers`) is enforced. A second booking by the same user for the same package and date returns 409; a party larger than the package allows returns 400. The price is (package + activities) × guests. Cancelling a booking frees its places.
    - 6 simultaneous single-guest bookings for a 3-place package give exactly 3 × 201 and 3 × 409.
    - A guest can cancel a confirmed booking more than 24 hours before check-in (`CancelledAt` is set). Inside the cutoff the guest gets 400; the provider can still cancel.
    - "Completed" is refused before the check-in date. Setting a booking to its current status is refused.
    - **DEF-010:** an unchanged save keeps a hotel Approved, and an admin edit keeps it Approved. An owner's content edit sets it to Pending, which hides it publicly (404) and blocks new bookings. Adding an activity to an approved package sets the package to Pending.
- Repeat runs of the two concurrency tests: 5/5 passed (`concurrency-repeat-5x.log`).
- `api-tests.log` contains no `fail:`, `500` or `Unhandled` entries. The concurrent losers are refused by the service-level check after the row lock, not by a database error.

## Manual smoke test

Run against the local development database (Development environment, `http://localhost:5080`):

- `GET /api/bookings/availability?roomId=6&checkIn=<today+15>&checkOut=<today+17>` returned `available: true`, 2 nights, total 24000.00.
- The same request with `checkIn=2020-01-01` returned `available: false` with the reason "Check-in date cannot be in the past.".
- `GET /api/bookings/availability?travelPackageId=2&checkIn=<today+15>&guests=2` returned `available: true`, 6 nights, total 1948.00 (2 × package total) and `remainingPlaces: 8`.
- Sending both `roomId` and `travelPackageId` returned 400 `application/problem+json` with `errors.roomId`.
