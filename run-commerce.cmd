@echo off
cd /d "%~dp0"
if not exist ".venv\Scripts\python.exe" (
  echo First create .venv and install requirements.txt as described in README.md.
  pause
  exit /b 1
)
".venv\Scripts\python.exe" -m commerce compare --seeds 1,2,3,4 --contexts separated,cheap_access,sparse,small_only,equal_assortment --plot --out outputs\commerce-demo
if errorlevel 1 (
  pause
  exit /b 1
)
start "" "%~dp0outputs\commerce-demo\comparison.png"
pause
