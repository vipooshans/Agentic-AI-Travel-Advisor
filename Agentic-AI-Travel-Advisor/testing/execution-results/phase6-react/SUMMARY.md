# Phase 6 - React + TypeScript web app (`web-react/`)

- **Date:** 2026-10-01
- **Base commit:** `22d6ad4` plus the uncommitted Phase 6 changes (committed right after this run)
- **Stack:** React 19, react-router-dom 7, Axios, Vite 8, TypeScript 6 (strict), ESLint 10 with the React hooks rules, Vitest 5 with jsdom, React Testing Library and MSW 2
- **Backend:** the Phase 5 API, run locally with `dotnet run --project web-api --urls http://localhost:5080 --environment Development` against the local Development PostgreSQL database
- **LLM:** no model key, so every assistant reply in the walkthrough came from the deterministic agents (`Rule-based` in the UI)
- **Commands** (run from `web-react/`; output saved next to this file):
  - `npm run typecheck`: `typecheck.log`
  - `npm run lint`: `lint.log`
  - `npm test`: `test.log`, plus `vitest run --reporter=verbose` in `test-verbose.log`
  - `npm run build`: `build.log`

The npm scripts call `node node_modules/<tool>/...` directly. The `&` in this repository's path breaks the `.cmd` shims that npm normally uses on Windows.

## What was built

| Area | Implementation |
|---|---|
| API layer | `src/api/client.ts` is a single Axios instance. It adds the bearer token, turns ProblemDetails into `ApiError` (detail, then the first field error, then the title, then a per-status default), and gives clear network and timeout messages. A 401 on any call except login clears the session and raises a session-expired event. `services.ts` covers every API area; `types.ts` and `enums.ts` mirror the DTOs and the numeric enums. |
| Session | Stored in `localStorage` (`ta.session`). Expired or corrupt sessions are dropped on load. |
| Routing / RBAC | `RequireRole` sends anonymous users to `/login` and users with the wrong role to `/forbidden`. Each role has its own home page and navigation. This only hides UI; the API enforces every rule itself (see the live checks below). |
| Public | Hotel and package search, hotel and package detail pages with reviews, a booking panel (availability check, then book), and login/register with client-side validation. |
| USER | The AI assistant has a conversation list, recent recommendations, a structured plan view with "Save as itinerary", and a proposal card with an explicit "Confirm booking" button. The button is shown only on the latest unexpired proposal. The booking card shows the status the backend returned. Also: My bookings (cancel, simulated payments, reviews for completed stays), Itineraries and Profile (contact details and travel preferences). |
| HOTEL_OWNER | Dashboard with statistics for their own listings, hotel CRUD, room CRUD with open/close, and booking management (confirm, complete, cancel). |
| TRAVEL_AGENT | Dashboard, package CRUD, package activities, transport route CRUD, and booking management. |
| ADMIN | Dashboard with platform statistics, hotel and package approvals, users (search, role filter, activate/deactivate, create HOTEL_OWNER or TRAVEL_AGENT accounts), destinations, review moderation and system settings. |
| Config | `VITE_API_BASE_URL` (empty means same origin through the Vite proxy) and `VITE_API_PROXY_TARGET` (default `http://localhost:5080`), documented in `.env.example`. The API's `Cors:AllowedOrigins` now includes `http://localhost:5173` and `http://localhost:4173`. No secrets were added. |

## Automated results (final run)

| Check | Result |
|---|---|
| `npm run typecheck` | exit 0, no errors |
| `npm run lint` | exit 0, no problems |
| `npm test` | **35 passed, 0 failed, 0 skipped** in 8 files |
| `npm run build` | exit 0; JS 414.06 kB (123.18 kB gzip), CSS 15.84 kB |

| File | Tests | Covers |
|---|---|---|
| `lib/lib.test.ts` | 7 | Plan-to-itinerary mapping, statistics keys by enum name or number, money and date formatting (including fractional amounts), optional numbers |
| `auth/session.test.ts` | 4 | Round trip, expired session dropped, corrupt JSON dropped, clear |
| `api/client.test.ts` | 6 | Bearer token sent, ProblemDetails `detail` used, validation field errors kept, unreachable server message, a 401 clears the session and raises the event, a failed login does not |
| `auth/RequireRole.test.tsx` | 5 | Anonymous → `/login`; USER, HOTEL_OWNER and TRAVEL_AGENT → `/forbidden` on admin pages; ADMIN allowed |
| `pages/public/LoginPage.test.tsx` | 4 | Role-based redirect after login, Enter-key submit, API error shown with nothing stored, empty-field validation without an API call |
| `pages/shared/ManageBookingsPage.test.tsx` | 3 | Confirm through `PATCH /status`, server reason shown when a transition is rejected, 403 shown as a permission error |
| `pages/user/MyBookingsPage.test.tsx` | 2 | Pays the outstanding amount, then "Paid in full" with no pay form; the pay form is hidden while a cash payment awaits confirmation |
| `pages/user/AssistantPage.test.tsx` | 4 | Plan rendered, then Confirm sends `confirmBookingId` and the conversation id. The UI shows Pending (never Confirmed) and the Confirm button disappears. A refusal shows no plan or booking controls. A 429 is shown. The composer is disabled when an admin turns the assistant off. |

## Live walkthrough (real API and database)

Done in the Cursor browser against `http://localhost:5173`, which proxies to the API on `http://localhost:5080`. Screenshots are in `screenshots/`.

**USER** (registered through the UI as `nimal.p6.react@example.com`):

1. Sent "Plan a 3-day trip to Ella for 2 people with a budget of LKR 100,000" and got a `plan` with 12 tool calls (`01`, `02`).
   - Total Rs. 96,000 of 100,000: Ella Gap View Inn Garden Double Rs. 16,000, Ella Hills Escape Rs. 72,000 and the Kandy-Ella train Rs. 8,000.
   - Two warnings name rooms that are already booked for those dates.
2. "Save as itinerary" showed "Saved to your itineraries". The Itineraries page lists "Ella trip, 9 items, LKR 96,000" (`08`).
3. "Book the hotel" returned a `booking_proposal` card with "Nothing is booked until you confirm" (`03`).
4. Clicking "Confirm booking" returned `booking_created`: booking #12 with status **Pending**, LKR 16,000 (`04`). My bookings showed #12 as Pending (`05`).
5. A simulated card payment returned Completed with a `SIM-...` reference (`06`).
6. Opening `/admin/users` as this user redirected to the 403 page (`07`).

**HOTEL_OWNER:**

- The dashboard loaded statistics for the owner's own listings (`09`).
- Bookings → Confirm on #12 showed "Booking #12 confirmed." and the row changed to Confirmed (`10`).
- My hotels listed the owner's hotels with their approval status (`11`).

**TRAVEL_AGENT:** the Packages page (`12`) and the Transport page (`13`) loaded the agent's own data.

**ADMIN:**

- The dashboard counts reflected the new confirmation: Confirmed went from 1 to 2 (`15`).
- Approvals listed the one pending package with Approve and Reject (`14`).
- The users list loaded (`16`) and the settings page rendered all 5 settings.

**USER again:** #12 now shows **Confirmed**. The payments panel shows "Paid in full." with no pay form (`17`).

### Server-side checks through the proxy (`api-proxy-checks.log`)

| Request | Result |
|---|---|
| `GET /api/users`, `/api/settings`, `/api/reports/statistics` with the USER token | 403 each |
| `GET /api/bookings` with the USER token | 200 |
| `GET /api/bookings` with no token | 401 |
| `GET /api/bookings/12` | status 1 (Confirmed), total 16000.00 |
| `GET /api/bookings/12/payments` | 1 payment, status 1 (Completed), amount 16000.00 |
| `POST /api/bookings/12/payments` again | 409 "This booking is already paid or has a payment awaiting confirmation." |

## Defects found and fixed during this phase

1. **AssistantPage race (found by Vitest).**
   - Symptom: after the first reply in a new conversation, the thread was wiped. A follow-up request then hit an unmocked `GET /api/ai/conversations/7`.
   - Cause: React Router 7 applies `setSearchParams` inside a transition, and the history-loading effect depended on extra state.
   - Fix: the effect now depends only on the URL id, and a ref skips reloading the conversation the page has just created.
2. **Impure render (found by ESLint `react-hooks/purity`).**
   - `Date.now()` was called while rendering the proposal card.
   - Fix: a `useNow` interval clock.
3. **CSS-injection risk in listing images.**
   - Inline `background-image` URLs were replaced with an `<img>` component that accepts only http(s) URLs.
4. **Table rows misaligned (found in the walkthrough).**
   - Cause: `td.row-actions` used `display: flex`, which takes the cell out of table layout and broke the row borders.
   - Fix: the cell stays `table-cell` with right-aligned inline buttons (`16`).
5. **Average booking shown as "LKR 10,368.5" (found in the walkthrough).**
   - Fix: `formatMoney` now prints 0 or 2 decimals (`15`), with a unit-test case added.
6. **Pay button stayed active after full payment (found in the walkthrough).**
   - The API already rejects a second payment with 409.
   - Fix: the panel now applies the same rule. It shows the outstanding amount, and "Paid in full" or "awaiting provider confirmation" once nothing is owed. Covered by `MyBookingsPage.test.tsx`.
7. **`client.test.ts` type errors (found by `npm run typecheck`).**
   - `.catch()` produced the type `ApiError | AxiosResponse`.
   - Fix: a typed `rejection()` helper. The tests passed before; only the type check failed.

Not a product defect: during the walkthrough, the browser automation's "press Enter" and `fill` on password inputs did not trigger React's handlers, so the buttons were clicked or the text typed instead. The new LoginPage test confirms that Enter submits the form.

## Side effects on the Development database

- New user `nimal.p6.react@example.com`.
- Booking #12 (Ella Gap View Inn Garden Double, 2026-10-15 to 2026-10-17), now Confirmed, with one completed simulated card payment.
- AI conversation 20 with its recommendations, and one saved itinerary.

## Known limitations

- There are no browser end-to-end tests yet. Playwright is planned for Phase 8; this phase's browser evidence is the manual walkthrough above.
- The production bundle is a single 414 kB chunk. Route-level code splitting has not been done.
- The MVC portal (`web-app/`) is kept as decided; the React app is the new client for all four roles.
