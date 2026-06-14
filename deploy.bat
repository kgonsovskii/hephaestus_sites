@echo off
setlocal
cd /d "%~dp0"
dotnet build "src\Sites.Deploy.Cli\Sites.Deploy.Cli.csproj" -v:q
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet run --project "src\Sites.Deploy.Cli\Sites.Deploy.Cli.csproj" --no-build -- %*
exit /b %ERRORLEVEL%
