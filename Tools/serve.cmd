@echo off
rem Serves Builds\WebGL at http://localhost:8080 and opens the browser.
rem Needs Python 3 (https://www.python.org). If you don't have it, use
rem 3DUI > Build > Build and Run locally (current scene) inside Unity instead.
cd /d "%~dp0"
start "" /b cmd /c "timeout /t 2 >nul & start http://localhost:8080"
where py >nul 2>nul && (py -3 serve.py %*) || (python serve.py %*)
