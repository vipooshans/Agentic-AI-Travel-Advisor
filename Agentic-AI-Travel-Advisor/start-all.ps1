# ===================================================================
# Agentic AI Travel Advisor - One-Click Full Project Runner
# ===================================================================
$ErrorActionPreference = "Stop"

Write-Host "`n🚀 Starting Agentic AI Travel Advisor..." -ForegroundColor Cyan

# Stop any previously running TravelAdvisor processes to release ports 5000 and 7000
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# 1. Ensure Postgres container is running
Write-Host "`n[1/4] Ensuring PostgreSQL database is running on port 5433..." -ForegroundColor Yellow
try {
    docker compose up -d postgres
} catch {
    Write-Host "Warning: Could not start docker postgres. If already running, continuing..." -ForegroundColor Gray
}

# 2. Locate or Install .NET 8 SDK
Write-Host "`n[2/4] Checking for .NET 8 SDK..." -ForegroundColor Yellow

function Test-DotNetSdk($path) {
    if (Test-Path $path) {
        $sdks = & $path --list-sdks 2>$null
        if ($sdks -match "8\.") {
            return $true
        }
    }
    return $false
}

$dotnetExe = $null

if (Test-DotNetSdk "dotnet") {
    $dotnetExe = "dotnet"
} elseif (Test-DotNetSdk "$env:ProgramFiles\dotnet\dotnet.exe") {
    $dotnetExe = "$env:ProgramFiles\dotnet\dotnet.exe"
} elseif (Test-DotNetSdk "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe") {
    $dotnetExe = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
}

if (-not $dotnetExe) {
    Write-Host "No .NET 8 SDK detected." -ForegroundColor Yellow
    Write-Host "Downloading official Microsoft .NET 8 SDK installer..." -ForegroundColor Cyan
    
    $installerUrl = "https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe"
    $installerPath = "$env:TEMP\dotnet-sdk-8-setup.exe"
    
    $curlExe = "$env:SystemRoot\System32\curl.exe"
    if (Test-Path $curlExe) {
        & $curlExe -L --progress-bar "$installerUrl" -o "$installerPath"
    } else {
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($installerUrl, $installerPath)
    }
    
    if (Test-Path $installerPath) {
        Write-Host "Installing .NET 8 SDK (a progress window will appear, please allow it)..." -ForegroundColor Cyan
        Start-Process -FilePath $installerPath -ArgumentList "/passive /norestart" -Wait
    }
    
    # Re-check dotnet
    if (Test-DotNetSdk "$env:ProgramFiles\dotnet\dotnet.exe") {
        $dotnetExe = "$env:ProgramFiles\dotnet\dotnet.exe"
    } elseif (Test-DotNetSdk "dotnet") {
        $dotnetExe = "dotnet"
    } else {
        Write-Error "Could not find dotnet after installation. Please install it from https://aka.ms/dotnet/download and re-run."
        exit 1
    }
}

Write-Host "Using .NET SDK: $dotnetExe" -ForegroundColor Green

# 3. Locate project folders
$rootDir = $PSScriptRoot
if (-not (Test-Path (Join-Path $rootDir "web-api"))) {
    $rootDir = (Get-Item $PSScriptRoot).Parent.FullName
}
$apiDir = Join-Path $rootDir "web-api"
$webDir = Join-Path $rootDir "web-app"

# Ensure PATH has the dotnet directory for child processes
$dotnetDir = Split-Path $dotnetExe -Parent
$env:PATH = "$dotnetDir;$env:PATH"

# 4. Synchronous Build to verify everything compiles cleanly and restores packages
Write-Host "`n[3/5] Restoring and Building Web API..." -ForegroundColor Yellow
& $dotnetExe build "$apiDir\TravelAdvisor.Api.csproj" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Web API build failed! Check errors above." -ForegroundColor Red
    pause
    exit 1
}

Write-Host "`n[4/5] Restoring and Building Web App..." -ForegroundColor Yellow
& $dotnetExe build "$webDir\TravelAdvisor.Web.csproj" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Web App build failed! Check errors above." -ForegroundColor Red
    pause
    exit 1
}

# 5. Launch Web API and Web App in separate visible windows so they stay active
Write-Host "`n[5/5] Launching servers..." -ForegroundColor Yellow

$apiCmd = "title TravelAdvisor API (Port 5000) && cd /d `"$apiDir`" && `"$dotnetExe`" run --no-build --launch-profile http"
$webCmd = "title TravelAdvisor Web App (Port 7000) && cd /d `"$webDir`" && `"$dotnetExe`" run --no-build --launch-profile http"

Start-Process "cmd.exe" -ArgumentList "/k `"$apiCmd`""
Start-Process "cmd.exe" -ArgumentList "/k `"$webCmd`""

Write-Host "Waiting for servers to become ready..." -ForegroundColor Cyan

# Poll until port 7000 is accepting connections
$ready = $false
for ($i = 0; $i -lt 25; $i++) {
    Start-Sleep -Seconds 1
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $tcp.Connect("127.0.0.1", 7000)
        $tcp.Close()
        $ready = $true
        break
    } catch {
        Write-Host "." -NoNewline
    }
}

Write-Host ""
$dashboardUrl = "http://localhost:7000/Customer/Dashboard"

Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host "🎉 SUCCESS! Travel Advisor is now live!" -ForegroundColor Green
Write-Host "👉 Dashboard: $dashboardUrl" -ForegroundColor Cyan
Write-Host "👉 API:       http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Green

Start-Process $dashboardUrl

