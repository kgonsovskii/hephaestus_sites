@echo off
setlocal
set /p PROFILE="Sites profile: "
if "%PROFILE%"=="" (
  echo Profile is required.
  exit /b 1
)
cd /d "%~dp0"
call "%~dp0deploy.bat" "%PROFILE%" %*
exit /b %ERRORLEVEL%
