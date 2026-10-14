# OWASP ZAP scans of the running API, in the zaproxy/zap-stable container.
#   baseline  passive scan of what the spider reaches from the API root (no attacks)
#   api       active scan of every operation in the OpenAPI document, without a token
#   api-user  the same active scan with the bearer token of a freshly registered USER account
# Reports: testing/security/zap/reports/zap-<mode>.{html,json,md} and testing/execution-results/phase9-nfr/zap-<mode>.log
# The API must run in Development (Swagger) and should run with raised rate limits, see testing/security/README.md.
param(
    [Parameter(Mandatory)][ValidateSet('baseline', 'api', 'api-user')][string]$Mode,
    [string]$HostApi = 'http://localhost:5080',
    [string]$ContainerApi = 'http://host.docker.internal:5080',
    [int]$MaxScanMinutes = 30,
    [string]$Image = 'zaproxy/zap-stable'
)
$ErrorActionPreference = 'Stop'
$security = $PSScriptRoot
$zapDir = Join-Path $security 'zap'
$reports = Join-Path $zapDir 'reports'
$results = Join-Path (Split-Path $security -Parent) 'execution-results\phase9-nfr'
New-Item -ItemType Directory -Force $reports, $results | Out-Null
$log = Join-Path $results "zap-$Mode.log"

$dockerArgs = @('run', '--rm', '-v', "${zapDir}:/zap/wrk:rw")
if ($Mode -eq 'api-user') {
    # A Unix-time suffix would be reported by ZAP as "Timestamp Disclosure" when /api/auth/me echoes the email.
    $email = "zap.user.$([Guid]::NewGuid().ToString('N').Substring(0, 10))@example.test"
    $password = 'Zap#Scan' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $auth = Invoke-RestMethod -Method Post "$HostApi/api/auth/register" -ContentType 'application/json' `
        -Body (@{ email = $email; password = $password; firstName = 'Zap'; lastName = 'Scanner' } | ConvertTo-Json)
    # Passed by name so the token is not on the docker command line; ZAP adds it to every request it sends.
    $env:ZAP_AUTH_HEADER_VALUE = "Bearer $($auth.token)"
    $dockerArgs += @('-e', 'ZAP_AUTH_HEADER_VALUE')
    "Scanning as $email (role USER)"
}

$reportArgs = @('-r', "reports/zap-$Mode.html", '-J', "reports/zap-$Mode.json", '-w', "reports/zap-$Mode.md")
$dockerArgs += $Image
if ($Mode -eq 'baseline') {
    $dockerArgs += @('zap-baseline.py', '-t', $ContainerApi) + $reportArgs
} else {
    $zapOptions = "-config scanner.maxScanDurationInMins=$MaxScanMinutes -config scanner.maxRuleDurationInMins=5"
    $dockerArgs += @('zap-api-scan.py', '-t', "$ContainerApi/swagger/v1/swagger.json", '-f', 'openapi', '-O', $ContainerApi, '-z', $zapOptions) + $reportArgs
}

$started = Get-Date
$ErrorActionPreference = 'Continue'
& docker @dockerArgs 2>&1 | ForEach-Object { "$_" } | Out-File -FilePath $log -Encoding utf8
$code = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
Remove-Item Env:ZAP_AUTH_HEADER_VALUE -ErrorAction SilentlyContinue

# Reports can echo request headers; the token belongs to a throwaway account but is still a credential.
foreach ($file in Get-ChildItem $reports -Filter "zap-$Mode.*") {
    $text = [IO.File]::ReadAllText($file.FullName)
    $redacted = $text -replace 'Bearer\s+eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+', 'Bearer <redacted>' `
                      -replace '"token"\s*:\s*"eyJ[^"]+"', '"token":"<redacted>"' `
                      -replace 'eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]+', '<redacted-jwt>'
    if ($redacted -ne $text) { [IO.File]::WriteAllText($file.FullName, $redacted, (New-Object Text.UTF8Encoding $false)) }
}

$status = "ZAP $Mode finished with exit code $code after $([int]((Get-Date) - $started).TotalMinutes) min (0 = no warnings, 1 = at least one FAIL, 2 = warnings only, 3 = error)."
Add-Content -Path $log -Value $status -Encoding utf8
"$status Log: $log"
exit $code
