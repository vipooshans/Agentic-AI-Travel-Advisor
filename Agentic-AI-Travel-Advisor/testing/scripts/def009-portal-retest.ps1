# DEF-009 manual retest: MVC portal against the real API.
# Signs in through the portal's login form, then replaces the stored API token with an invalid one
# (what the portal sees after the token expires or the API's signing key changes).
param(
    [string]$Portal = 'http://localhost:7000',
    [string]$Email = 'owner@traveladvisor.com',
    [string]$Password = $env:PORTAL_OWNER_PASSWORD
)
$ErrorActionPreference = 'Stop'
if (-not $Password) { throw 'Set PORTAL_OWNER_PASSWORD (Development demo owner password).' }
Add-Type -AssemblyName System.Net.Http

$cookies = New-Object System.Net.CookieContainer
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = $cookies
$handler.AllowAutoRedirect = $false
$http = New-Object System.Net.Http.HttpClient($handler)
$http.BaseAddress = [Uri]$Portal

function Send([string]$Path) {
    $response = $http.GetAsync($Path).Result
    [pscustomobject]@{ Code = [int]$response.StatusCode; Location = "$($response.Headers.Location)" }
}

function Cookie-Names {
    ($cookies.GetCookies([Uri]$Portal) | Where-Object { $_.Value -and -not $_.Expired } | ForEach-Object Name) -join ', '
}

$loginPage = $http.GetStringAsync('/Account/Login').Result
$token = [regex]::Match($loginPage, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
$form = New-Object 'System.Collections.Generic.Dictionary[string,string]'
$form['Email'] = $Email
$form['Password'] = $Password
$form['__RequestVerificationToken'] = $token
$login = $http.PostAsync('/Account/Login', (New-Object System.Net.Http.FormUrlEncodedContent($form))).Result
"Sign in through the portal form             -> $([int]$login.StatusCode) Location: $($login.Headers.Location)"
"   cookies: $(Cookie-Names)"

$r = Send '/Owner/Hotels'
"1. GET /Owner/Hotels with a valid token        -> $($r.Code)"

$r = Send '/Owner/EditHotel/999999'
"2. GET /Owner/EditHotel/999999 (API 404)       -> $($r.Code)"

$cookies.GetCookies([Uri]$Portal)['access_token'].Value = 'expired.or.rotated.token'
$r = Send '/Owner/Hotels'
"3. GET /Owner/Hotels with an invalid API token -> $($r.Code) Location: $($r.Location)"
"   cookies left after the redirect: $(Cookie-Names)"

$r = Send $r.Location
"4. Follow the redirect                         -> $($r.Code) (login page shown, not bounced back to the dashboard)"
