# Checks every evidence reference in testing/test-cases/TEST-CASES.md against the recorded result files.
# A reference is written `file` :: `name`, with file relative to testing/execution-results.
#   .trx  - every test whose name contains `name` must have the outcome the Status column claims
#   .xml  - JUnit: matching testcases (or testsuites, for Newman requests) must have no failure, error or skip
#   .jsonl - Dart/Flutter JSON reporter: every matching test must have result success (or be skipped for Not executed)
#   other - a line containing `name` must exist; for Pass, no matching line may carry a failure marker
# Exits 1 if any reference is missing, does not match, or contradicts the Status column.
param(
    [string]$Catalogue = (Join-Path $PSScriptRoot '..\test-cases\TEST-CASES.md'),
    [string]$Results = (Join-Path $PSScriptRoot '..\execution-results')
)
$ErrorActionPreference = 'Stop'

function Read-Text([string]$path) {
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) { return [Text.Encoding]::Unicode.GetString($bytes, 2, $bytes.Length - 2) }
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { return [Text.Encoding]::UTF8.GetString($bytes, 3, $bytes.Length - 3) }
    return [Text.Encoding]::UTF8.GetString($bytes)
}

function Read-DartTestJson([string]$path) {
    $names = @{}
    $results = [Collections.Generic.List[object]]::new()
    foreach ($line in (Read-Text $path) -split "`r?`n") {
        if (-not $line.Trim()) { continue }
        $event = $line | ConvertFrom-Json
        if ($event.type -eq 'testStart') { $names[$event.test.id] = $event.test.name }
        elseif ($event.type -eq 'testDone' -and -not $event.hidden) {
            $outcome = if ($event.skipped) { 'skipped' } else { $event.result }
            $results.Add([pscustomobject]@{ Name = $names[$event.testID]; Outcome = $outcome })
        }
    }
    return , $results
}

$cache = @{}
function Get-Doc([string]$path) {
    if (-not $cache.ContainsKey($path)) {
        $cache[$path] = if ($path -match '\.(trx|xml)$') { [xml](Read-Text $path) }
                        elseif ($path -like '*.jsonl') { Read-DartTestJson $path }
                        else { (Read-Text $path) -split "`r?`n|`r" }
    }
    return $cache[$path]
}

$expectedTrxOutcome = @{ 'Pass' = 'Passed'; 'Fail' = 'Failed'; 'Not executed' = 'NotExecuted' }
$refPattern = [regex]'`([^`]+)`\s*::\s*`([^`]+)`'
$problems = [Collections.Generic.List[string]]::new()
$ids = @{}
$rows = 0; $refs = 0

foreach ($line in Get-Content $Catalogue -Encoding utf8) {
    if ($line -notmatch '^\|\s*([A-Z]+-\d{3})\s*\|') { continue }
    $id = $Matches[1]
    $cells = $line.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    if ($cells.Count -ne 9) { $problems.Add("${id}: expected 9 columns, found $($cells.Count)"); continue }
    if ($ids.ContainsKey($id)) { $problems.Add("${id}: duplicate test ID") }
    $ids[$id] = $true
    $rows++
    $status = $cells[7]
    if (-not $expectedTrxOutcome.ContainsKey($status)) { $problems.Add("${id}: unknown status '$status'"); continue }

    $found = $refPattern.Matches($cells[8])
    if ($found.Count -eq 0) { $problems.Add("${id}: no evidence reference"); continue }
    foreach ($m in $found) {
        $refs++
        $file = $m.Groups[1].Value; $name = $m.Groups[2].Value
        $path = Join-Path $Results $file
        if (-not (Test-Path $path)) { $problems.Add("${id}: file not found: $file"); continue }
        $doc = Get-Doc $path

        if ($file -like '*.trx') {
            $hits = @($doc.TestRun.Results.UnitTestResult | Where-Object { $_.testName.Contains($name) })
            if ($hits.Count -eq 0) { $problems.Add("${id}: no test containing '$name' in $file"); continue }
            $wrong = @($hits | Where-Object { $_.outcome -ne $expectedTrxOutcome[$status] })
            if ($wrong.Count) { $problems.Add("${id}: '$name' in $file has outcome $($wrong[0].outcome), catalogue says $status") }
        }
        elseif ($file -like '*.xml') {
            $cases = @($doc.SelectNodes('//testcase') | Where-Object { $_.name.Contains($name) })
            if ($cases.Count -eq 0) {
                foreach ($suite in @($doc.SelectNodes('//testsuite') | Where-Object { $_.name.Contains($name) })) { $cases += @($suite.SelectNodes('testcase')) }
            }
            if ($cases.Count -eq 0) { $problems.Add("${id}: no testcase or testsuite containing '$name' in $file"); continue }
            $failed = @($cases | Where-Object { $_.SelectSingleNode('failure|error|skipped') })
            if ($status -eq 'Pass' -and $failed.Count) { $problems.Add("${id}: '$name' in $file has $($failed.Count) failed or skipped case(s)") }
            if ($status -eq 'Fail' -and -not $failed.Count) { $problems.Add("${id}: '$name' in $file passed, catalogue says Fail") }
        }
        elseif ($file -like '*.jsonl') {
            $hits = @($doc | Where-Object { $_.Name -and $_.Name.Contains($name) })
            if ($hits.Count -eq 0) { $problems.Add("${id}: no test containing '$name' in $file"); continue }
            $wanted = if ($status -eq 'Pass') { 'success' } elseif ($status -eq 'Not executed') { 'skipped' } else { $null }
            $wrong = @($hits | Where-Object { if ($wanted) { $_.Outcome -ne $wanted } else { $_.Outcome -eq 'success' } })
            if ($wrong.Count) { $problems.Add("${id}: '$name' in $file has result $($wrong[0].Outcome), catalogue says $status") }
        }
        else {
            $hits = @($doc | Where-Object { $_.Contains($name) })
            if ($hits.Count -eq 0) { $problems.Add("${id}: no line containing '$name' in $file"); continue }
            # Logs captured through the Windows console hold the k6 and Vitest failure marks (U+2717, U+00D7)
            # as their UTF-8 bytes read in code page 437: \u0393\u00A3\u00F9 and \u251C\u00F9.
            $marked = @($hits | Where-Object { $_ -match '\[E\]|\u00D7|\u2717|\u0393\u00A3\u00F9|\u251C\u00F9' })
            if ($status -eq 'Pass' -and $marked.Count) { $problems.Add("${id}: '$name' in $file is marked as failed: $($marked[0].Trim())") }
        }
    }
}

"Checked $rows test cases and $refs evidence references in $(Resolve-Path $Catalogue)"
if ($problems.Count) {
    $problems | ForEach-Object { "PROBLEM: $_" }
    "$($problems.Count) problem(s) found"
    exit 1
}
'All evidence references resolve and agree with the Status column.'
