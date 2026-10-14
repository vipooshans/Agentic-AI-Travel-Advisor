# Testing Guide: how to run the tests and read the reports

This guide explains how the Agentic AI Travel Advisor & Booking System is tested, how to run every test
suite yourself, where each suite writes its report, and how to open and read that report. All tests run
against the real system: the ASP.NET Core API, PostgreSQL, the React web app and the Flutter app. There is no
separate testing application.

## 1. What is tested, and the latest results

The final regression run was made on 2026-10-01 on the final code (branch `dev`). These numbers come from the
recorded files in `testing/execution-results/phase9-nfr/`.

| Suite | Tool | Result of the final run |
|---|---|---|
| Backend unit tests | xUnit + Moq | 411 passed, 0 failed, 35 not executed (live-LLM cases, see section 6) |
| API, database and security tests | WebApplicationFactory + Testcontainers PostgreSQL 16 | 130 passed, 0 failed |
| API contract suite | Postman collection run with Newman | 79 requests, 315 assertions, 0 failed |
| React unit and page tests | Vitest + React Testing Library + MSW | 286 passed in 17 files |
| Browser end-to-end tests | Playwright (Chromium) on the real API and database | 17 passed |
| Flutter widget and service tests | flutter_test + mocktail | 94 passed |
| Flutter integration test | integration_test with `flutter drive` on the real API | 3 passed |
| Load tests | k6 | all thresholds passed in all 4 scripts |
| Security scans | OWASP ZAP | API scans: 118 rules passed, 0 warnings |

Test documentation:

| Document | What it contains |
|---|---|
| Test plan (`testing/test-plan/TEST-PLAN.md`) | Scope, test levels, environments, entry and exit criteria, evidence rules |
| Test-case catalogue (`testing/test-cases/TEST-CASES.md`) | 108 test cases: 107 Pass, 1 Not executed. Each with ID, feature, preconditions, steps, input, expected, actual, status and evidence |
| Defect log (`testing/defect-reports/DEFECT-LOG.md`) | 27 defects (1 Critical, 5 High, 12 Medium, 9 Low), each with steps, expected, actual, evidence, fix and retest result. All are retested and closed; DEF-020 is closed with a documented limitation |
| Phase summaries (`testing/execution-results/phase*/SUMMARY.md`) | What each phase ran and what it found |

All of these documents are reproduced in full in the appendices of this PDF.

## 2. Before you start

### 2.1 Software

| Needed for | Software |
|---|---|
| Backend tests | .NET 8 SDK |
| API tests, k6, ZAP | Docker Desktop (running) |
| Running the API | PostgreSQL 16 with the `travel_advisor` database |
| React tests, Playwright, Newman | Node.js 22.12 or newer |
| Flutter tests | Flutter 3 SDK; Chrome and a matching ChromeDriver for the integration test |

### 2.2 Windows: map the project to a drive letter

The `&` in the folder name `Agentic AI Travel Advisor & Booking System` breaks `npx`, Gradle and the Flutter
tool scripts. Map the project folder to a drive letter once per session and run Node and Flutter commands
from that drive:

```powershell
subst T: "E:\project\Agentic AI Travel Advisor & Booking System\Agentic-AI-Travel-Advisor"
```

In this guide, `T:\` means the `Agentic-AI-Travel-Advisor` folder.

### 2.3 Secrets

The API needs a database connection string and a JWT key (see the main README). They are kept in .NET user
secrets, never in git. The Playwright database checks and the k6 room-usage script read the same connection
string from `E2E_DB_CONNECTION`. To load it into the current PowerShell window without printing it:

```powershell
$line = dotnet user-secrets list --project web-api | Where-Object { $_ -like 'ConnectionStrings:DefaultConnection = *' }
$env:E2E_DB_CONNECTION = $line.Substring('ConnectionStrings:DefaultConnection = '.Length)
```

### 2.4 Start the API for the tests that need it

Newman, Playwright, the Flutter integration test, k6 and ZAP call a running API on port 5080. These tools
sign in many times from one IP address, so raise the rate limits for the test session:

```powershell
$env:RateLimiting__AuthPermitsPerMinute = '100000'
$env:RateLimiting__AiPermitsPerMinute = '100000'
dotnet run --project web-api --urls http://localhost:5080 --environment Development
```

Check that it is up: open `http://localhost:5080/api/health` (it should say `healthy`) or the Swagger UI at
`http://localhost:5080/swagger`. Stop the API with Ctrl+C when you are done, and start it without these
variables for normal use, so the default limits (10 sign-ins and 20 AI requests per minute) apply again.

The backend unit tests, the API tests (they start their own PostgreSQL container), Vitest and the Flutter
widget tests do not need a running API.

## 3. Running each suite and opening its report

Where a command writes reports into a `TestResults` folder, that folder is ignored by git, so your local runs
never overwrite the recorded evidence.

### 3.1 Backend unit tests (xUnit + Moq)

```powershell
dotnet test tests/TravelAdvisor.UnitTests --results-directory TestResults `
  --logger "trx;LogFileName=unit-tests.trx" --logger "html;LogFileName=unit-tests.html"
```

**Where the report is:** `TestResults/unit-tests.html` and `TestResults/unit-tests.trx`.

**How to see it:**
- The console ends with a line such as `Passed! - Failed: 0, Passed: 411, Skipped: 35, Total: 446`.
- Open `unit-tests.html` in any browser. It lists every test with its outcome and duration; expand a failed
  test to see the assertion message and stack trace.
- The `.trx` file opens in Visual Studio (File > Open > File), which shows it in the Test Explorer. It is also
  plain XML: each `<UnitTestResult>` has a `testName` and an `outcome` of `Passed`, `Failed` or `NotExecuted`.

<!-- screenshot:unit-html -->

The 35 skipped tests are the live-LLM evaluation cases. They run only when `AI_EVAL_API_KEY` is set (section 6).

### 3.2 API, database and security tests (WebApplicationFactory + Testcontainers)

Docker must be running; the tests start a throwaway PostgreSQL 16 container.

```powershell
dotnet test tests/TravelAdvisor.Api.Tests --results-directory TestResults `
  --logger "trx;LogFileName=api-tests.trx" --logger "html;LogFileName=api-tests.html"
```

**Where the report is:** `TestResults/api-tests.html` and `.trx`, read the same way as section 3.1. The final
recorded run is `testing/execution-results/phase9-nfr/backend-api-tests-final.trx` (130 passed).

These tests cover authentication, the access-control matrix (every endpoint against every role), ownership
(one provider cannot change another's data), validation, error format, security headers, rate limiting,
booking concurrency, database constraints and migrations, and the AI agent end to end.

### 3.3 React unit and page tests (Vitest)

```powershell
cd T:\web-react
npm install
npm test -- --reporter=verbose --reporter=junit --outputFile.junit=TestResults/vitest-junit.xml
npm run lint
npm run typecheck
```

**How to see the result:**
- The `verbose` reporter prints every test with a tick or a cross, then a summary such as
  `Test Files 17 passed (17)` and `Tests 286 passed (286)`.
- `TestResults/vitest-junit.xml` is a JUnit XML report. The first line,
  `<testsuites name="vitest tests" tests="286" failures="0" errors="0" ...>`, gives the totals. Any failed
  test has a `<failure>` element with the message.

### 3.4 Browser end-to-end tests (Playwright)

Playwright drives Chromium through the React app against the real API and PostgreSQL. If the API (port 5080)
or the Vite dev server (port 5173) is not already running, Playwright starts them itself.

```powershell
cd T:\web-react
# load E2E_DB_CONNECTION first (section 2.3) so the tests can also check the stored rows
npm run e2e
```

**Where the report is:**
- `web-react/playwright-report/index.html`: the HTML report.
- `web-react/test-results/junit.xml`: JUnit XML.
- `web-react/test-results/<test name>/`: a screenshot and a trace for every failed test.

**How to see it:** run the report viewer, which opens the report in your browser:

```powershell
cd T:\web-react
node node_modules/@playwright/test/cli.js show-report
```

The report lists every test with a green or red mark and its duration. Click a test to see its steps. For a
failed test, the page shows the error, the screenshot, and a **Trace** link that replays every action, network
request and DOM snapshot. A trace file can also be opened directly:

```powershell
node node_modules/@playwright/test/cli.js show-trace test-results/<test folder>/trace.zip
```

<!-- screenshot:playwright -->

### 3.5 Flutter widget and service tests (mocktail)

```powershell
cd T:\mobile-app
flutter pub get
flutter analyze
flutter test --reporter expanded --file-reporter json:TestResults/flutter-test.jsonl
```

**How to see the result:**
- The console prints a running count and ends with `All tests passed!` (or the failures).
- `TestResults/flutter-test.jsonl` holds one JSON event per line. A `testStart` event gives each test's `id`
  and `name`; the matching `testDone` event gives its `result` (`success`, `failure` or `error`). This file is
  the complete per-test record, because the console reporter does not print every test name.

### 3.6 Flutter integration test (real API, Chrome)

1. Start the API on port 5080 (section 2.4).
2. Start ChromeDriver on port 4444 (it must match your Chrome version):
   `chromedriver --port=4444`
3. Run the traveler journey:

```powershell
cd T:\mobile-app
flutter drive --driver=test_driver/integration_test.dart `
  --target=integration_test/traveler_journey_test.dart `
  -d web-server --browser-name=chrome --profile --web-port 4173 `
  --dart-define=API_BASE_URL=http://localhost:5080
```

**How to see the result:** the console ends with `All tests passed.` when the 3 tests pass, or prints the
failing test and its error. Save the console output to keep a record, for example by adding
`*> TestResults\flutter-integration.log` to the command.

### 3.7 API contract suite (Postman + Newman)

The collection sends 79 requests to the running API, covering every role, the booking workflow, access
control (RBAC and IDOR), validation and error responses. Every response is also checked for leaked secrets and
stack traces.

```powershell
cd T:\testing\integration\postman
npm install
npm test
```

**Where the report is:**
- `testing/integration/postman/reports/newman-report.html`: the HTML report.
- `testing/integration/postman/reports/newman-junit.xml`: JUnit XML.

Both are ignored by git.

**How to see it:**
- The console prints each request with its status code and a tick or cross per assertion, then a summary table
  (requests, test scripts, assertions; executed and failed).
- Open `newman-report.html` in a browser. The summary page shows totals and failures. The **Requests** tab lists
  every request; expand one to see the request, the response and each assertion. Sensitive data (tokens,
  passwords) is left out of the report on purpose.

You can also import `TravelAdvisor.postman_collection.json` and `local.postman_environment.json` into the
Postman app and run the collection there.

<!-- screenshot:newman -->

### 3.8 Load tests (k6)

k6 runs in Docker. Start the API with the raised limits first (section 2.4), then from `T:\`:

```powershell
.\testing\performance\run-k6.ps1 -Script catalog-search   # or: login, booking, ai-chat
```

**Where the report is:** in `testing/execution-results/phase9-nfr/`:
- `k6-<script>.log`: the console summary.
- `k6-<script>-summary.json`: every metric.

Running a script **overwrites** the recorded file of the same name, so commit or copy the old one first if you
want to keep it.

**How to read it:**
- **THRESHOLDS** at the top of the log: each line is a pass criterion, for example `p(95)<500`, with the
  measured value. A tick means it passed.
- **checks**: the percentage of response checks that passed (correct data, not just status 200).
- **http_req_duration**: response times (average, median, p90, p95, max).
- **http_req_failed**: the error rate.
- `run-k6.ps1` exits with 0 when every threshold passed and 99 when any failed.

Note: the logs recorded on Windows show the tick as `Γ£ô` and the cross as `Γ£ù`. The console wrote them
in an old code page; the meaning is the same.

### 3.9 Security scans (OWASP ZAP)

ZAP runs in Docker. Start the API with the raised limits (section 2.4), then from `T:\`:

```powershell
.\testing\security\run-zap.ps1 -Mode baseline   # passive scan
.\testing\security\run-zap.ps1 -Mode api        # active attacks on every API operation, no token
.\testing\security\run-zap.ps1 -Mode api-user   # the same, signed in as a new USER
```

**Where the report is:**
- `testing/security/zap/reports/zap-<mode>.html` (also `.json` and `.md`): the full report.
- `testing/execution-results/phase9-nfr/zap-<mode>.log`: the console summary.

These files are overwritten by a new run.

**How to see it:**
- The log lists every rule as `PASS`, `WARN-NEW` or `FAIL-NEW` and ends with totals such as
  `FAIL-NEW: 0  FAIL-INPROG: 0  WARN-NEW: 0  WARN-INPROG: 0  INFO: 0  IGNORE: 0  PASS: 118`.
- Open the HTML report in a browser. It starts with a summary of alerts by risk (High, Medium, Low,
  Informational). Each alert shows its description, the affected URLs, the evidence and the recommended
  solution.
- The script exits with 0 for no warnings, 1 for a failure, 2 for warnings only and 3 for an error.
- After an `api-user` run, check that the scan really was signed in: admin and provider endpoints must have
  answered 403, not 401 (see `testing/security/README.md`).

<!-- screenshot:zap -->

### 3.10 Check the test-case catalogue against the evidence

```powershell
powershell -ExecutionPolicy Bypass -File testing/scripts/verify-test-cases.ps1
```

The script reads every row of `TEST-CASES.md`, opens each referenced evidence file and checks that the named
test exists and that its recorded outcome matches the Status column. It ends with
`All evidence references resolve and agree with the Status column.`, or lists each problem and exits with 1.

## 4. Where the recorded results are

Every run that counts as evidence is stored under `testing/execution-results/`, one folder per phase. Failed
runs are kept next to the run that followed the fix; nothing is deleted or re-typed.

| Folder | Contents |
|---|---|
| `phase0-baseline` | The state of the original project before any change |
| `phase1-backend-arch` … `phase7-flutter` | The test runs of each implementation phase |
| `phase8-tests` | The full test suites (backend, database, AI evaluation, React, Playwright, Flutter, end to end, Newman), including the before-fix runs of DEF-009 and DEF-021 to DEF-025 |
| `phase9-nfr` | k6, ZAP, the DEF-026 and DEF-027 retests and the final regression (`*-final.*`, `e2e-full-suite-rerun.*`) |

File types and how to open them:

| Extension | What it is | How to open it |
|---|---|---|
| `.trx` | .NET test results (XML) | Visual Studio, or any text editor; look for `outcome=` |
| `.xml` (JUnit) | Newman, Playwright, Vitest results | Any text editor; `<failure>` marks a failed test |
| `.jsonl` | Flutter JSON reporter events | Any text editor; see section 3.5 |
| `.log` / `.txt` | Console output of a run | Any text editor |
| `.html` | Newman, ZAP and Playwright reports | A web browser |
| `.md` | Summaries and the AI evaluation report | A Markdown viewer (VS Code, GitHub) or a text editor |
| `.json` | k6 metrics, ZAP alerts, AI evaluation details | A text editor or a JSON viewer |

## 5. How to read a test case and a defect

**A test case** (in `TEST-CASES.md`) is one table row:

| Column | Meaning |
|---|---|
| Test ID | Area and number, e.g. `BOOK-004` |
| Feature | What is being tested |
| Preconditions | What must be true before the test |
| Test Steps | What the automated test does |
| Input | The data sent |
| Expected Result | What the test asserts |
| Actual Result | What the recorded run produced |
| Status | `Pass`, `Fail` or `Not executed` |
| Evidence | `file` :: `test name`, where the file is relative to `testing/execution-results/` |

To check a case yourself: open the evidence file and search for the test name; the outcome next to it must
match the Status.

**A defect** (in `DEFECT-LOG.md`) has an ID and title, then severity and priority, description, steps to
reproduce, expected and actual behaviour, evidence, status, the fix, and the retest result with a link to the
run that proves the fix. The defect-retest table at the end of the test-case catalogue links each defect to
the test that now guards against it.

## 6. Tests that were not executed

The 35 live-LLM evaluation cases (test case AI-018) were not executed. They send the AI evaluation scenarios
to a real OpenAI-compatible model and run only when the `AI_EVAL_API_KEY` environment variable holds a working
key. No working key was available. The API's `Ai:ApiKey` user secret is a placeholder that the provider
rejects. The evaluation's scripted cases, which use the deterministic planner and a scripted stand-in for the
model, did run: 71 of 71 passed (`testing/execution-results/phase8-tests/ai-evaluation/ai-evaluation-report.md`).
No result is claimed for a live model.

To run them when you have a key:

```powershell
$env:AI_EVAL_API_KEY = '<your key>'          # optional: $env:AI_EVAL_BASE_URL, $env:AI_EVAL_MODEL
$env:AI_EVAL_REPORT_DIR = "$PWD\TestResults\ai-eval"   # use a full path
dotnet test tests/TravelAdvisor.UnitTests --filter AiLiveEvaluationTests
```

The report is written to `TestResults/ai-eval/ai-evaluation-live-report.md` and `.json`. Setting
`AI_EVAL_REPORT_DIR` on a normal unit-test run also writes `ai-evaluation-report.md`, the report for the
scripted cases.
