# Bug Log

## Confirmed application defects

None confirmed. The application could not be started, and no application test completed, so this run does not establish that the application is defect-free.

## Execution blockers and observations

These are test-environment/tooling observations, not confirmed product defects.

### ENV-001 — PowerShell unavailable

- **Impact:** Could not run the documented `dotnet`, `npm`, `flutter`, Docker, or Git commands from the terminal, and could not start the API or clients.
- **Reproduction:** Attempt to start a PowerShell-backed command session.
- **Observed:** `PowerShell is not available. Tried "pwsh.exe", "powershell.exe".` Both executable spawns returned `ENOENT`.
- **Disposition:** Test commands and application startup marked Not run. A working shell is needed to continue.

### ENV-002 — Test-runner integration did not locate the existing test files

- **Impact:** React and Flutter test-runner attempts did not execute test cases or produce test counts.
- **Reproduction:** Request the available test runner to run the repository's existing React test file set, then the Flutter `mobile-app/test/` file set.
- **Observed:** For each request, the tool returned `No tests found in the files. Ensure the correct absolute paths are passed to the tool.`
- **Disposition:** This does not mean the repository has no tests; the README, project manifests, and source tree document and contain existing suites. Run them with their documented native commands from a working terminal.

### ENV-003 — No local application service responded

- **Impact:** UI and API behavior could not be manually exercised.
- **Reproduction:** With no application started, navigate to the documented local ports.
- **Observed:** `localhost:5173` and `localhost:5080/swagger` returned `ERR_FAILED (-2)`; `localhost:7000` returned `ERR_CONNECTION_REFUSED (-102)`.
- **Disposition:** Not treated as a product bug because the application-start command could not be executed.

## Warnings / unverified prerequisites

- Installed .NET SDK, Node.js/npm, Flutter/Dart, Docker daemon, database availability, and browser versions were not verified.
- The API's documented secret/database prerequisites and the API integration suite's Docker/PostgreSQL requirement were not checked.
- The app, API validation behavior, database persistence, and external AI provider behavior remain unverified.
