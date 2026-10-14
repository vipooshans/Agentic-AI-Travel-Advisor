# Security testing

Security is tested at three levels, all against the real API:

| Level | Where | What it covers |
|---|---|---|
| Automated API tests | `tests/TravelAdvisor.Api.Tests` (`SecurityHardeningTests`, `AccessControlMatrixTests`, `OwnershipAndContractTests`, `AiAgentTests`) | Authentication, JWT validation, role policies on every endpoint, ownership (IDOR), lockout, rate limiting, ProblemDetails without internals, security headers, AI injection and exfiltration |
| API contract suite | `testing/integration/postman` (Newman) | RBAC and IDOR requests, tampered and missing tokens; every response is checked for secrets and stack traces |
| OWASP ZAP | `testing/security/run-zap.ps1` (this folder) | Passive checks and active attacks (SQL injection, XSS, command injection, path traversal, SSTI, XXE, …) on every operation in the OpenAPI document, without a token and as a USER |

## Running the ZAP scans

ZAP runs in the official `zaproxy/zap-stable` Docker image.

1. Start the API on port 5080 in Development, so that the OpenAPI document at `/swagger/v1/swagger.json` is available. Raise the rate limits, otherwise ZAP's requests are answered with 429 and the attacks never reach the endpoints:

   ```powershell
   $env:RateLimiting__AuthPermitsPerMinute = '100000'
   $env:RateLimiting__AiPermitsPerMinute = '100000'
   dotnet run --project web-api --no-build --urls http://localhost:5080 --environment Development
   ```

2. Run the scans from `Agentic-AI-Travel-Advisor/`:

   ```powershell
   .\testing\security\run-zap.ps1 -Mode baseline   # passive scan of what the spider finds from the API root
   .\testing\security\run-zap.ps1 -Mode api        # active scan of every OpenAPI operation, no token
   .\testing\security\run-zap.ps1 -Mode api-user   # the same, as a freshly registered USER
   ```

   - `api-user` registers a throwaway USER account and passes its token to ZAP through the `ZAP_AUTH_HEADER_VALUE` environment variable, which adds it to every request. The token is not put on the command line.
   - Afterwards the script replaces anything that looks like a JWT in the reports with `<redacted>`.
   - The scan is deliberately **not** run as ADMIN. An active scan replays every endpoint with attack payloads, and as admin that would delete catalog data and deactivate accounts.

3. Output:
   - `testing/security/zap/reports/zap-<mode>.html|json|md`: the full ZAP reports.
   - `testing/execution-results/phase9-nfr/zap-<mode>.log`: the console summary, with PASS, WARN or FAIL for each rule.

   The script exits with ZAP's code: 0 means no warnings, 1 means at least one FAIL, 2 means warnings only, 3 means an error.

## How to check that a scan was really authenticated

ZAP does not report whether the header was used, and a scan that silently runs without the token looks almost the same. Check two things after an `api-user` run:
- **Statuses.** In `zap-api-user.json`, the alert "A Client Error response code was returned" lists the statuses. Admin and provider endpoints must return **403**, not 401.
- **Footprint.** The scan account's data must have changed, for example its name is replaced by ZAP's payloads.

The first `api-user` run on 2026-10-01 failed this check: its 63 responses were 401. It is kept as `zap-api-user-run1-token-not-applied.*` and is not counted as an authenticated result.

Results and triage: `testing/execution-results/phase9-nfr/SUMMARY.md`, section 9b.
