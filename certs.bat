@echo off
setlocal
cd /d "%~dp0"

if "%~1"=="" goto banner
if /i "%~1"=="publish" goto banner
goto run

:banner
echo Issue Let's Encrypt certificate for all domains in sites.json
echo Output: cert\sites.pfx
echo Requires: DNS points to this machine, port 80 free, Sites.Host stopped
echo.

:run
if "%~1"=="" (
  dotnet run --no-launch-profile --project src\Sites.CertTool\Sites.CertTool.csproj -- publish
) else (
  dotnet run --no-launch-profile --project src\Sites.CertTool\Sites.CertTool.csproj -- %*
)
exit /b %ERRORLEVEL%
