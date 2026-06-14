@echo off
setlocal
cd /d "%~dp0"

set "ASPNETCORE_ENVIRONMENT=Development"
set "SITE_NAME="

if "%~1"=="" goto run
if /i "%~1"=="--sitename" (
  if "%~2"=="" (
    echo Error: --sitename requires a value.
    echo Usage: run.bat [--sitename ^<targetHost^>]
    echo   No args: all sites from sites.json
    echo   --sitename tube-18.xyz: single-site mode
    exit /b 1
  )
  set "SITE_NAME=%~2"
  goto run
)

set "SITE_NAME=%~1"

:run
if defined SITE_NAME (
  dotnet run --no-launch-profile --project src\Sites.Host\Sites.Host.csproj -- %SITE_NAME%
) else (
  dotnet run --no-launch-profile --project src\Sites.Host\Sites.Host.csproj
)
exit /b %ERRORLEVEL%
