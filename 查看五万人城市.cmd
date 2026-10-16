@echo off
setlocal
chcp 65001 >nul
call "%~dp0启动HarborCity.cmd" "%~dp0Temp\CityScenarios\challenge50000-connectivity-after\final.hcity"
exit /b %errorlevel%
