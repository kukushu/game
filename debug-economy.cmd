@echo off
cd /d "%~dp0"
".venv\Scripts\python.exe" -m econlab shell --trace
pause
