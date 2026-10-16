@echo off
cd /d "%~dp0"
set "PS_PATH=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%PS_PATH%" set "PS_PATH=C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"

"%PS_PATH%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-all.ps1"
pause
