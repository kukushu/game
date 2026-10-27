@echo off
powershell.exe -NoProfile -File "%~dp0Stop-EconomyWeb.ps1"
if errorlevel 1 pause
