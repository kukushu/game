@echo off
setlocal
chcp 65001 >nul
call "%~dp0启动HarborCity.cmd" "%~dp0Temp\CityScenarios\challenge30000-traffic10000\threshold.hcity"
exit /b %errorlevel%
