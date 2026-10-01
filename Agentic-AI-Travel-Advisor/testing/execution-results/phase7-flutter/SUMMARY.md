# Phase 7 - Flutter traveler app (`mobile-app/`)

- **Date:** 2026-10-01
- **Base commit:** `b992493` plus the uncommitted Phase 7 changes (committed right after this run)
- **Toolchain:** Flutter 3.47.5 (stable), Dart test with `package:http/testing.dart` `MockClient`; provider, go_router 14, http, flutter_secure_storage 9.2.4
- **Backend:** the Phase 6 API, run locally with `dotnet run --project web-api --urls http://localhost:5080 --environment Development` against the local Development PostgreSQL database
- **LLM:** no model key, so assistant replies came from the deterministic agents
- **Commands** (run from `mobile-app/`; output saved next to this file):
  - Baseline before any change: `baseline-analyze-test.log` (1 analyzer warning, 2 tests passed)
  - `flutter analyze`: `final-analyze.log`
  - `flutter test --reporter expanded`: `final-test.log`
  - `flutter test test_live --reporter expanded --dart-define=API_BASE_URL=http://localhost:5080`: `final-live-api-test.log`

The `&` in this repository's path breaks Gradle's `gradlew.bat` and the Flutter tool's Windows shims. Builds were therefore run from a `subst` drive (`T:\` mapped to `Agentic-AI-Travel-Advisor\`), so the logs show `T:/mobile-app/...` paths.

## What was built

| Area | Implementation |
|---|---|
| API layer | `ApiService` takes an injectable `http.Client`, base URL and timeout. Every request times out (20 s by default; the AI chat uses 90 s). Query values are encoded, and null or blank ones are dropped. ProblemDetails become readable messages: several field errors are listed, otherwise `detail`, then `message`, then a per-status default. Timeouts and offline errors have their own messages. |
| Role gating (DEF-014) | Only `USER` accounts can sign in. Staff accounts get "This app is for travelers…" and nothing is stored. A stored staff session is discarded on start-up. |
| Session expiry (DEF-014) | A 401 on an authenticated request signs the user out, clears secure storage and shows "Your session has expired. Please sign in again." on the login screen. Failed sign-ins are excluded. The AI chat is cleared on sign-out. |
| Validation | Email, password policy (matching the API's Identity rules), names, a confirm-password field, guests 1-50, notes ≤ 1000 characters, positive prices, and booking dates (no past check-in; check-out after check-in). |
| Catalog | Hotel and package search by keyword and maximum price through the API's `q` / `maxPrice`. Ratings and "from LKR" prices on cards. Hotel and package detail screens show reviews, and package and destination screens show "Getting there" transport options. |
| Booking | "Check availability" calls `/api/bookings/availability` and shows the quote (nights, guests, total) or the server's reason. "Request booking for LKR X" is enabled only after an available quote, and changing any input asks for a new check. The snackbar shows the backend status (Pending). |
| My bookings | Cancel with a confirmation dialog for Pending or Confirmed bookings. Completed stays can be reviewed once (1-5 stars, comment ≤ 2000). |
| AI assistant | Renders the structured plan: cost against budget, stay, package, activities, transport, day by day, warnings and assumptions, plus "Save itinerary". Booking proposals show "Nothing is booked until you confirm". Confirm is offered only on the latest unexpired proposal and sends `confirmBookingId`. The result card shows the status the backend returned and never claims a confirmation the backend did not make. |
| Money | `formatMoney` matches the web app (`LKR 16,000`, `LKR 10,368.50`). |
| Config | `API_BASE_URL` dart-define. The default is `http://10.0.2.2:5000` on the Android emulator and `http://localhost:5000` elsewhere, including web. No secrets were added. |

## Automated results (final run)

| Check | Result |
|---|---|
| `flutter analyze` | **No issues found** (the baseline `unused_local_variable` warning in `auth_service.dart` is gone) |
| `flutter test` | **65 passed, 0 failed, 0 skipped** |
| `flutter test test_live` (real API) | **9 passed, 0 failed** |

| File | Tests | Covers |
|---|---|---|
| `api_service_test.dart` | 14 | URL building and encoding, ProblemDetails parsing, bearer token, 401 → expired session (not for sign-in or anonymous calls), timeouts, offline, `ensureSuccess` |
| `auth_test.dart` | 12 | Traveler sign-in stored; HOTEL_OWNER / TRAVEL_AGENT / ADMIN refused with nothing stored; staff session discarded on restore; registration error shown; provider expiry notice on 401 and on start-up; notice cleared on re-login; chat cleared on sign-out |
| `chat_provider_test.dart` | 5 | Booking only after confirming the latest proposal (`confirmBookingId`, conversation id, backend status Pending shown); expired proposal; 429; saving the plan; reset |
| `models_test.dart` | 14 | Money formatting, structured plan parsing and itinerary mapping, proposal expiry, chat history, catalog models, validators |
| `screens_test.dart` | 18 | Plan card and over-budget chip, proposal states, booking result, refusal, login and register validation, staff message, session-expired notice, review sheet and lists, the booking flow (quote, invalidation, 409 reason), booking list review and cancel, **cancel refreshes the list** (added for DEF-018), search |
| `widget_test.dart` | 2 | The two original tests |

The live suite (`test_live/`, tag `live`) is kept out of the default run because it needs the API and seeded data. It signs in once in `setUpAll` because the API allows 10 sign-ins per minute per IP. It covers:

- an owner refused
- wrong credentials (401)
- token expiry
- 403 on an admin API
- hotel search by keyword and price
- package search by per-person price
- reviews, transport and the traveler's own data
- availability, then a booking for 2 guests, then cancellation
- the assistant returning a structured plan, not `booking_created`

## Visual walkthrough (Flutter **web** build in Chrome, real API)

The Android emulator (`emulator-5554`) stayed `offline` even after an adb restart and a cold boot with software rendering, so the app could not be run on Android in this environment. Instead the same code was built for web (`flutter run -d web-server --web-port 4173 --dart-define=API_BASE_URL=http://localhost:5080`) and driven in the Cursor browser through Flutter's semantics tree. Screenshots are in `screenshots/`:

1. `owner@traveladvisor.com` sign-in shows the travelers-only message and stays on the login screen (`01`).
2. Traveler `nimal.p6.react@example.com` signs in and lands on Home (`02`).
3. The hotels list shows "from LKR" prices (`03`).
4. Hotel detail shows rooms with LKR prices and the Reviews section ("No reviews yet") (`04`). The hotel name over the photo was unreadable; see fixes below.
5. Booking the Garden Double for 8-11 Oct: "Check availability" showed "Available · 3 nights · 1 guest · Total LKR 24,000", and the button changed to "Request booking for LKR 24,000" (`05`).
6. Submitting showed "Booking #15 created. Status: Pending." and the list showed #15 as Pending (`06`).
7. Cancel booking opened the confirmation dialog (`07`). After confirming, "Booking cancelled" appeared but the card still said Pending (`08`), although `GET /api/bookings/15` returned `status: 2`. Logged as **DEF-018** and fixed.
8. After the fix and a rebuild, the hotel name is readable (`09`). Booking #16 was created (`10`) and cancelled; the card changed to Cancelled in place (`11`).
9. The suggestion "Plan a 3-day trip to Ella under Rs. 50,000." returned a structured plan with day-by-day items. It included three warnings from the availability tool about rooms already booked for those dates, the assumptions made, and "Save itinerary" (`12`).

### Not covered visually

- **Typing into the search fields:** text entry into Flutter web fields through the automation was unreliable after the first field on a screen. Focus jumped to the Destinations tab and the typed text never reached Flutter. After four attempts this was stopped. Keyword and price search are covered by `screens_test.dart` ("keywords and a maximum price are sent to the API") and by the live API test.
- **The top of the plan card** (cost against budget and the budget chip) was not captured: scrolling the Flutter canvas from the automation did not move it. It is covered by the "the plan card shows costs against budget…" widget test.
- **Android-specific behaviour** (secure storage on Android, the `10.0.2.2` default URL) was not exercised on a device.

## Defects found and fixed during this phase

1. **DEF-014 (open since the baseline).** Wrong-role login, no 401 handling, and missing timeouts. Fixed and closed; see `testing/defect-reports/DEFECT-LOG.md`.
2. **DEF-018 (found in the walkthrough).** Lists did not refresh after cancel, review, Retry or pull-to-refresh in debug builds.
   - Cause: `setState(() => _future = _load())` returns a `Future`, so `setState`'s debug assertion threw before the rebuild was scheduled.
   - Fix: all 13 call sites were changed.
   - Regression check: the new widget test failed before the fix and passes after it.
3. **Hotel and package name unreadable on the detail header (found in the walkthrough).**
   - Cause: the `FlexibleSpaceBar` title used Material 3's dark default text colour over the darkened photo.
   - Fix: the title is now white (`catalog_detail_view.dart`), visible in `09` versus `04`.
4. **Analyzer warnings in `test_live/`.**
   - Cause: `setMockInitialValues` is marked visible-for-testing, and the analyzer only treats `test/` as test code.
   - Fix: the file now carries a documented `ignore_for_file`.
5. **Web-unsafe config.** `ApiConfig` used `dart:io` `Platform`, which throws on web. It now uses `kIsWeb` and `defaultTargetPlatform`.
6. **Live package-search test bug.** The test compared the API's `maxPrice` against the base price, but the API filters on the per-person total (price plus activities). The test was corrected to use `totalPrice`.

**Not product defects:**

- A first live re-run hit 429 on sign-in because of the auth rate limit. The suite now signs in once.
- In the walkthrough, an arrow glyph first rendered as an empty box while Flutter web loaded its fallback font. Once the font loaded it rendered normally, so no code change was kept.

## Side effects on the Development database

- Bookings #15 and #16 (Ella Gap View Inn Garden Double, 2026-10-08 to 2026-10-11, 1 guest) were created and then cancelled through the app.
- Each live-suite run creates and then cancels one 2-guest booking. The cancelled June and July 2027 bookings visible in `06` come from those runs.
- AI conversations were added by the live suite and the walkthrough.

## Known limitations

- No run on an Android device or emulator in this environment (see above).
- Flutter `integration_test` and mocktail-based tests are planned for Phase 8; this phase uses `MockClient` fakes and widget tests.
