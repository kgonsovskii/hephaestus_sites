@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "MSG=%~1"
if not defined MSG set "MSG=Update"

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
  echo Error: not a git repository.
  exit /b 1
)

for /f "usebackq tokens=*" %%B in (`git rev-parse --abbrev-ref HEAD`) do set "BRANCH=%%B"
if /I "%BRANCH%"=="HEAD" (
  echo Error: detached HEAD; checkout a branch first.
  exit /b 1
)

git status --porcelain | findstr /r "." >nul 2>&1
if not errorlevel 1 (
  git add -A
  git commit -m "%MSG%"
  if errorlevel 1 exit /b 1
)

git rev-parse "@{u}" >nul 2>&1
if errorlevel 1 (
  git push -u origin "%BRANCH%"
) else (
  git push origin "%BRANCH%"
)
if errorlevel 1 exit /b 1

git ls-remote --exit-code origin "refs/heads/%BRANCH%" >nul 2>&1
if errorlevel 1 (
  echo Error: branch '%BRANCH%' not found on origin after push.
  exit /b 1
)

echo OK: pushed %BRANCH%.
echo Branches: https://gitverse.ru/sevenseals/acs/branches
exit /b 0
