# Phase 4 - Role features (API)

- **Date:** 2026-10-01
- **Base commit:** `4c763f6` plus the uncommitted Phase 4 changes (committed right after this run)
- **Database:** Testcontainers `postgres:16-alpine` (fresh container per run)
- **Command:**
  - `dotnet test tests/TravelAdvisor.UnitTests --logger "trx;LogFileName=unit-tests.trx"`
  - `dotnet test tests/TravelAdvisor.Api.Tests --logger "trx;LogFileName=api-tests.trx" --logger "console;verbosity=normal"`

## New endpoints

| Area | Endpoints | Who |
|---|---|---|
| Reviews | `GET /api/hotels/{id}/reviews`, `GET /api/packages/{id}/reviews` | Public (visible reviews only) |
| | `GET /api/reviews` | Admin: all. Provider: visible reviews of own listings. User: own reviews |
| | `POST /api/reviews`, `PUT /api/reviews/{id}` | USER (completed bookings only, one review per booking, author only) |
| | `DELETE /api/reviews/{id}` | Author or admin |
| | `PATCH /api/reviews/{id}/status` | Admin (moderation) |
| Transportation | `GET /api/transportation`, `GET /api/transportation/{id}` | Public (active options only) |
| | `GET /api/transportation/mine`, `POST`, `PUT /{id}`, `DELETE /{id}` | TRAVEL_AGENT (own options and own packages only) or admin |
| System settings | `GET /api/settings/public` | Public |
| | `GET /api/settings`, `PUT /api/settings/{key}` | Admin. Only known keys are accepted, and each value is validated |
| Payments (simulated) | `GET /api/bookings/{id}/payments` | Guest, listing provider or admin |
| | `POST /api/bookings/{id}/payments` | The guest who booked |
| | `PATCH /api/bookings/{id}/payments/{paymentId}/status` | Listing provider or admin |
| Profile | `GET/PUT /api/users/me/profile` | Any signed-in user |
| Statistics | `GET /api/reports/statistics?from&to&top` | Providers (own listings) and admin (platform) |

Settings now change behaviour:
- `Ai.AssistantEnabled=false` makes `POST /api/ai/chat` return 503.
- `Booking.MaxAdvanceDays` and `Booking.GuestCancellationCutoffHours` are read by the booking rules added in Phase 3.

Cancelling a booking refunds its completed simulated payments and voids any pending ones.

## Results

- Unit tests: 56 passed, 0 failed, 0 skipped (`unit-tests.trx`, `unit-tests.log`).
  - 27 new cases are in `FeatureValidatorTests`:
    - the Luhn card check
    - card numbers rejected on cash and bank-transfer payments
    - the 24-hour `HH:mm` departure time format
    - transport limits
    - profile phone, currency and avatar URL rules
- API integration tests: 38 passed, 0 failed, 0 skipped (`api-tests.trx`, `api-tests.log`).
  - All 31 tests from earlier phases still pass.
  - 7 new tests are in `RoleFeatureTests`:
    - **Reviews:**
      - Reviewing a booking that isn't completed gets 400. Another user's booking gets 404, and a rating of 6 gets 400.
      - The first review gets 201 and a duplicate gets 409.
      - The public list shows "Test U." and never the reviewer's email, and the hotel's average rating updates.
      - A stranger editing the review gets 403. The guest or owner trying to moderate gets 403.
      - After an admin hides the review, it disappears from the public list, the hotel average and the owner's view. The author and admin can still see it.
    - **Package reviews:** the review is attached to the package, not a hotel.
    - **Transportation:**
      - A USER creating an option gets 403. Departure time `25:00` gets 400, and attaching another agent's package gets 403.
      - The destination is inherited from the package.
      - Another agent updating or deleting the option gets 403.
      - Once deactivated, the option disappears from public search, and an anonymous GET returns 404. The provider can still see it in `mine`. Delete returns 204.
    - **Settings:**
      - Public read gets 200. A USER reading or writing gets 403.
      - An unknown key gets 404. Bad values (`abc` for the advance-days setting, `RUPEES` for currency) get 400.
      - Turning the AI off makes chat return 503. Setting advance days to 30 makes a booking 40 days ahead return 400. Both settings are restored afterwards and chat returns 200 again.
    - **Payments:**
      - Invalid card gets 400. A card number on a cash payment gets 400. A stranger paying or listing payments gets 403.
      - The decline test card produces a `Failed` payment. A valid card produces `Completed` for the full amount with a `SIM-` reference. Paying again gets 409.
      - Card digits never appear in API responses.
      - Cash payments stay `Pending`. The guest or another owner confirming gets 403; the listing owner confirming works.
      - Cancelling the booking refunds the card payment, and paying a cancelled booking gets 400.
    - **Profile:**
      - The default profile comes back for a new user, and a full save round-trips.
      - A bad phone number, a future date of birth or a `javascript:` avatar URL gets 400.
      - A hotel owner can also save a profile. An anonymous request gets 401.
    - **Statistics:**
      - For an owner with 2 bookings (1 confirmed at 10000, 1 cancelled): total 2, revenue 10000, cancellation rate 0.5, 12 monthly buckets, and one top listing.
      - Another owner sees 0. Admin sees at least 2. A USER gets 403.
      - `top=0` gets 400, and `to` before `from` gets 400.

## Fix made during this phase

- The first full run passed, but `api-tests.log` contained one `fail: ... Unhandled exception` entry with a stack trace. It came from the deliberate 503 in the AI-disabled test, because `ApiExceptionHandler` logged every status of 500 or above as an error.
- Deliberate `AppException` 503s are now logged as `Request failed with 503`. Truly unexpected exceptions are still logged as errors.
- The results above come from the rerun after that change: 38/38 passed, with 0 `fail:`, `Unhandled` or 500 entries in the log.

## Manual smoke test

Run against the local development database (Development environment, `http://localhost:5080`):

- `GET /api/settings/public` returned LKR, AI enabled, 365 advance days and a 24 h cutoff.
- `GET /api/transportation?from=Kandy&to=Ella` returned the seeded train (mode 1) departing 08:47, 2000.00 per person, destination Ella.
- `GET /api/reports/statistics?top=3` as the demo owner returned 3 bookings, revenue 24500.00, cancellation rate 0.3333 and 12 months. Top listings were Ella Gap View Inn (24000.00) and Le Grand Paris Hotel (500.00).
