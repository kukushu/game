@echo off
setlocal
chcp 65001 >nul
call "%~dp0启动HarborCity.cmd" "%~dp0Temp\CityScenarios\challenge10000-commute\inflight.hcity"
exit /b %errorlevel%
