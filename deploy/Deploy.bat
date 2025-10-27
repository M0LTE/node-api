@echo off
REM Quick deployment helper - double-click to deploy
echo Starting remote deployment...
echo.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Deploy-Remote.ps1"
echo.
pause
