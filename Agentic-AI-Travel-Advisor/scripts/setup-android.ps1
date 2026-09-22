# Generates android/ then applies cleartext HTTP overlay for demo APKs.
# Usage (from mobile-app):  powershell -File ../scripts/setup-android.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$app = Join-Path $root "mobile-app"

Set-Location $app
flutter create . --platforms=android --org com.traveladvisor --project-name travel_advisor

$xmlDir = Join-Path $app "android\app\src\main\res\xml"
New-Item -ItemType Directory -Force -Path $xmlDir | Out-Null
Copy-Item (Join-Path $app "android-overlay\network_security_config.xml") (Join-Path $xmlDir "network_security_config.xml") -Force

$manifest = Join-Path $app "android\app\src\main\AndroidManifest.xml"
$text = Get-Content $manifest -Raw
if ($text -notmatch "usesCleartextTraffic") {
    $text = $text -replace "<application", "<application android:usesCleartextTraffic=`"true`" android:networkSecurityConfig=`"@xml/network_security_config`""
    Set-Content -Path $manifest -Value $text -NoNewline
}

Write-Host "Android platform ready. Build with:"
Write-Host "  flutter build apk --release --dart-define=API_BASE_URL=http://YOUR_LAN_IP:5000"
