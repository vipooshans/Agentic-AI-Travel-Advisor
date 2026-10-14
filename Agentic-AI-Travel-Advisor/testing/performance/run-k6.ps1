# Runs one k6 script from testing/performance/k6 in the grafana/k6 container against the API on the host.
# Output: testing/execution-results/phase9-nfr/k6-<script>.log and k6-<script>-summary.json
param(
    [Parameter(Mandatory)][ValidateSet('catalog-search', 'login', 'booking', 'ai-chat')][string]$Script,
    [string]$BaseUrl = 'http://host.docker.internal:5080',
    [string]$Image = 'grafana/k6:latest'
)
$ErrorActionPreference = 'Stop'
$testing = Split-Path $PSScriptRoot -Parent
$results = Join-Path $testing 'execution-results\phase9-nfr'
New-Item -ItemType Directory -Force $results | Out-Null

$runId = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
$log = Join-Path $results "k6-$Script.log"

# k6 writes progress to stderr; Windows PowerShell would treat those lines as errors under 'Stop'.
$ErrorActionPreference = 'Continue'
docker run --rm `
    -v "${testing}:/testing" `
    -w /testing/performance/k6 `
    -e BASE_URL=$BaseUrl -e RUN_ID=$runId `
    -e OWNER_EMAIL -e OWNER_PASSWORD -e ADMIN_EMAIL -e ADMIN_PASSWORD `
    $Image run --quiet `
    --summary-mode full `
    --summary-trend-stats 'avg,min,med,p(90),p(95),p(99),max' `
    --summary-export "/testing/execution-results/phase9-nfr/k6-$Script-summary.json" `
    "$Script.js" 2>&1 | ForEach-Object { "$_" } | Out-File -FilePath $log -Encoding utf8
$code = $LASTEXITCODE
$ErrorActionPreference = 'Stop'

# The export includes setup()'s return value, which holds the access tokens of the run's traveler accounts.
$summary = Join-Path $results "k6-$Script-summary.json"
if (Test-Path $summary) { & (Join-Path $PSScriptRoot 'redact-k6-summary.ps1') -Path $summary }

"k6 $Script finished with exit code $code (log: $log)"
exit $code
