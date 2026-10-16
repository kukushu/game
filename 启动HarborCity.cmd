@echo off
setlocal
chcp 65001 >nul
set "GAME_DIR=%~dp0Temp\RoadCore\Player"
if exist "%~dp0Temp\ManualCityPlayer\HarborCity.exe" set "GAME_DIR=%~dp0Temp\ManualCityPlayer"
set "LOG_DIR=%~dp0Temp\ManualPlay"
set "CITY_SAVE=%~dp0Temp\CityScenarios\challenge100000-dotnet64\final.hcity"
if not "%~1"=="" set "CITY_SAVE=%~f1"
if not exist "%GAME_DIR%\HarborCity.exe" goto missing
if not exist "%GAME_DIR%\HarborCity_Data\Managed\Assembly-CSharp.dll" goto missing
if not exist "%CITY_SAVE%" goto nosave
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
if not exist "%LOG_DIR%" goto logfailed
set "GAME_LOG=%LOG_DIR%\Player-%RANDOM%-%RANDOM%.log"
echo 正在打开 HarborCity 城市现场，初始暂停……
echo 加载存档：%CITY_SAVE%
echo 不启动自动测试，不自动退出，不覆盖已有城市存档。
echo 本次运行日志：%GAME_LOG%
start "" /D "%GAME_DIR%" "%GAME_DIR%\HarborCity.exe" -harbor-city-inspect "%CITY_SAVE%" -screen-fullscreen 0 -screen-width 1440 -screen-height 900 -logFile "%GAME_LOG%"
if errorlevel 1 goto launchfailed
exit /b 0
:missing
echo 没有找到完整的游戏构建：
echo %GAME_DIR%
echo 请让开发者重新构建 Player；此脚本不会自行修改工程或下载程序。
pause
exit /b 1
:nosave
echo 没有找到现场存档：%CITY_SAVE%
echo 不会改为打开空白城市。请把这个问题告诉开发者。
pause
exit /b 1
:logfailed
echo 无法创建日志目录：%LOG_DIR%
pause
exit /b 1
:launchfailed
echo 游戏启动失败，请将这个窗口里的信息告诉开发者。
pause
exit /b 1
