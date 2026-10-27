@echo off
cd /d "%~dp0"
if not exist ".venv\Scripts\python.exe" (
  echo Run python -m venv .venv and .venv\Scripts\python -m pip install -r requirements.txt first.
  pause
  exit /b 1
)
".venv\Scripts\python.exe" -m econlab compare --plot --out outputs\demo
if errorlevel 1 (
  pause
  exit /b 1
)
start "" "%~dp0outputs\demo\comparison.png"
pause
