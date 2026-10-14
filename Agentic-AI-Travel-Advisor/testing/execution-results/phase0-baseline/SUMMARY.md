# Phase 0 - Baseline (before any changes)

Executed: 2026-10-01, local workstation, against the code at commit `4833828`.

| Suite | Command | Result |
|-------|---------|--------|
| .NET unit | `dotnet test TravelAdvisor.slnx` | 19 passed, 0 failed |
| .NET API integration | `dotnet test TravelAdvisor.slnx` (local PostgreSQL 18, shared `travel_advisor_test` DB) | 8 passed, 0 failed |
| Flutter | `flutter test` | 2 passed, 0 failed |

Raw output: `dotnet-test.log`, `flutter-test.log`, `baseline.trx` (the TRX was overwritten by the second test project in the same run, so the log is the authoritative record).

Observations:
- The first run failed to build because an orphaned `TravelAdvisor.Api` process locked `bin/` files; after stopping it the run succeeded.
- API tests depended on a hard-coded local database password and a persistent database (DEF-001, DEF-016).
