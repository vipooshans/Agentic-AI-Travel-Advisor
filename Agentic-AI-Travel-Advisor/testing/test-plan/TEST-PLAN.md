# Test Plan - Agentic AI Travel Advisor & Booking System

## 1. Scope

All testing runs against the real system (ASP.NET Core API, PostgreSQL, Agentic AI service, React web app, Flutter app). There is no separate testing application.

In scope: authentication, role-based authorization, hotels, rooms, availability, packages, activities, transportation, bookings, payments (simulated), reviews, admin functions, AI planning/recommendation/itinerary/booking tools, AI safety, performance, and security.

Out of scope: real payment gateways, email delivery, iOS builds (no `ios/` project).

## 2. Test levels and tools

| Level | Tooling | Location |
|-------|---------|----------|
| Backend unit | xUnit, Moq | `tests/TravelAdvisor.UnitTests` |
| Backend integration / API | xUnit, WebApplicationFactory, Testcontainers (PostgreSQL 16) | `tests/TravelAdvisor.Api.Tests` |
| Database | xUnit + Testcontainers, raw Npgsql/EF | `tests/TravelAdvisor.Api.Tests/Database` |
| Agentic AI evaluation | xUnit (deterministic planner + scripted LLM via Moq) | `tests/TravelAdvisor.UnitTests/AI`, `tests/TravelAdvisor.Api.Tests/AI` |
| React web | Vitest, React Testing Library, MSW | `web-react/src/**/*.test.tsx` |
| Web E2E | Playwright | `web-react/e2e` |
| Flutter | flutter_test, mocktail | `mobile-app/test` |
| Flutter integration | integration_test | `mobile-app/integration_test` |
| API collection | Postman / Newman | `testing/integration/postman` |
| Performance | k6 (Docker image `grafana/k6`) | `testing/performance` |
| Security | OWASP ZAP (Docker image `zaproxy/zap-stable`) + xUnit security tests | `testing/security` |

## 3. Environments

- Local Windows 11 workstation, .NET SDK 8.0.423, Node 24, Flutter 3.47.5, Docker 29.
- API integration tests create a fresh PostgreSQL container per run. If Docker is unavailable they fall back to a uniquely named database on a local PostgreSQL (needs `TEST_PG_PASSWORD`), which is dropped afterwards.
- AI tests run with no LLM key (deterministic planner) so results are reproducible. The LLM tool-calling path is exercised through a scripted `ILlmClient` mock.

## 4. Entry / exit criteria

- Entry: solution builds, migrations apply to an empty database.
- Exit: all automated suites pass, every recorded defect is either fixed and retested or explicitly deferred with justification, and evidence is stored under `testing/execution-results` and `testing/evidence`.

## 5. Test design techniques

Equivalence partitioning and boundary values (dates, prices, capacity, budget), state transition testing (booking lifecycle, approval lifecycle), role/permission matrices (RBAC and ownership), negative testing (invalid tokens, injection payloads), and scenario-based AI evaluation.

## 6. Defect management

Defects are logged in `testing/defect-reports/DEFECT-LOG.md` with severity, priority, reproduction steps, evidence, fix, and retest result. Failing tests are not hidden or skipped to make a run green.

## 7. Evidence rules

Only real execution output is stored: TRX/JUnit reports, console logs, coverage files, k6 summaries, ZAP reports, and Playwright reports. Each phase has a folder in `testing/execution-results`.
