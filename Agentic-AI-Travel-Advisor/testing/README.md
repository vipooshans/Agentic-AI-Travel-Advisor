# Testing

Test documentation and evidence for the Agentic AI Travel Advisor. Automated test code lives next to the code it tests; this folder holds plans, cases, defects, scripts for cross-cutting tests, and recorded results.

```
testing/
├── test-plan/          Test plan and strategy
├── test-cases/         Test case catalogue (ID, feature, steps, expected/actual, status, evidence)
├── defect-reports/     Defect log with fixes and retest results
├── execution-results/  Per-phase run summaries and raw reports (TRX, logs, coverage)
├── evidence/           Screenshots and other artefacts referenced by test cases
├── backend/            Notes and how-to for xUnit/Moq/WebApplicationFactory suites
├── database/           Database test notes
├── web/                React test notes (Vitest/RTL/MSW/Playwright)
├── mobile/             Flutter test notes
├── integration/        End-to-end workflow and Postman/Newman collection
├── ai/                 Agentic AI evaluation scenarios and results
├── performance/        k6 scripts and results
└── security/           OWASP ZAP configuration, reports and security test notes
```

Rule: only real execution output is recorded here. Failed runs are kept alongside the retest that followed the fix.
