@echo off
rem Entry point for scripts\test.ps1. Batch is exempt from the PowerShell
rem execution policy, which blocks .ps1 files on a stock Windows install.
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\test.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if defined CI goto :quit
echo %CMDCMDLINE% | findstr /i /c:"/c" >nul 2>&1 && pause
:quit
exit /b %EXITCODE%
