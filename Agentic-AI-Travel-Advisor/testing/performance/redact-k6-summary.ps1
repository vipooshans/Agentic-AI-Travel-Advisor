# Removes "setup_data" (access tokens of the run's throwaway accounts) from k6 --summary-export files.
# Metrics and thresholds are kept unchanged.
param([Parameter(Mandatory)][string[]]$Path)
$ErrorActionPreference = 'Stop'

foreach ($file in $Path) {
    $summary = Get-Content $file -Raw | ConvertFrom-Json
    if (-not ($summary.PSObject.Properties.Name -contains 'setup_data')) { continue }
    $summary.PSObject.Properties.Remove('setup_data')
    # Windows PowerShell escapes <, >, & and ' as \uXXXX; restore them so thresholds such as p(95)<500 stay readable.
    $json = ($summary | ConvertTo-Json -Depth 32) -replace '\\u003c', '<' -replace '\\u003e', '>' -replace '\\u0026', '&' -replace '\\u0027', "'"
    [IO.File]::WriteAllText((Resolve-Path $file), $json, (New-Object Text.UTF8Encoding $false))
    "redacted setup_data from $file"
}
