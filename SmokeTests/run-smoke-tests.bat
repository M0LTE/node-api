@echo off
REM Smoke test runner script for Windows
REM Usage: run-smoke-tests.bat
REM Note: Configure target in appsettings.json before running

setlocal enabledelayedexpansion

set CONFIG_FILE=%~dp0appsettings.json

echo =====================================
echo   Node-API Smoke Test Runner
echo =====================================
echo.

REM Check if config exists
if not exist "%CONFIG_FILE%" (
    echo Error: appsettings.json not found
    echo Please create appsettings.json with your target configuration
    exit /b 1
)

echo Using configuration from appsettings.json
echo.
echo Configuration:
type "%CONFIG_FILE%"
echo.

REM Run the tests
echo Running smoke tests...
echo.

dotnet test --logger "console;verbosity=normal" --nologo
set TEST_RESULT=%ERRORLEVEL%

echo.
if %TEST_RESULT%==0 (
    echo =====================================
    echo   + All smoke tests PASSED
    echo =====================================
) else (
    echo =====================================
    echo   X Some smoke tests FAILED
    echo =====================================
)

exit /b %TEST_RESULT%
