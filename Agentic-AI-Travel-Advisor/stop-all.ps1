Get-Process -Name "TravelAdvisor.Api", "TravelAdvisor.Web", "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "TravelAdvisor" -or $_.Path -match "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Travel Advisor processes stopped." -ForegroundColor Green
