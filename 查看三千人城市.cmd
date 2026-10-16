@echo off
setlocal
chcp 65001 >nul
call "%~dp0启动HarborCity.cmd" "%~dp0Temp\CityScenarios\stage2-grid3000-normal\inflight.hcity"
exit /b %errorlevel%
