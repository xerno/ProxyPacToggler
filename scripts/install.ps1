# Tests, builds, installs into %LOCALAPPDATA%/Programs/ProxyPacToggler, registers
# the app to run at logon and launches it. Current user only, no admin rights.
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$NoAutoStart,
    [switch]$DebugBuild
)

$ErrorActionPreference = 'Stop'

# This script lives in scripts/; the repository root is one level up.
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$root = Split-Path -Parent $here
. (Join-Path $here 'build-config.ps1')

# --- Test ---------------------------------------------------------------------
# First: a red suite must not cost the time of a build nobody will install.
if (-not $SkipTests) {
    & (Join-Path $here 'test.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed, nothing was built. Use -SkipTests to override.' }
    Write-Host ''
}

# --- Build --------------------------------------------------------------------
if ($DebugBuild) {
    & (Join-Path $here 'build.ps1')
} else {
    & (Join-Path $here 'build.ps1') -Release
}
Write-Host ''

# --- Stop a running instance --------------------------------------------------
$built = Join-Path $root "bin\$AppName.exe"
if (-not (Test-Path $built)) { throw "Built binary not found: $built" }

$running = Get-Process $AppName -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Stopping the running $AppName..."
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# --- Install ------------------------------------------------------------------
Write-Host "Installing to $InstallDir..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item $built (Join-Path $InstallDir "$AppName.exe") -Force

$installed = Join-Path $InstallDir "$AppName.exe"

# --- Autostart ----------------------------------------------------------------
if (-not $NoAutoStart) {
    # Let the app register itself, so there is one implementation of this.
    Start-Process $installed -ArgumentList '--install' -Wait -NoNewWindow
    Write-Host 'Registered to start at logon.'
}

# --- Launch -------------------------------------------------------------------
Write-Host "Launching $AppName..."
Start-Process $installed
Start-Sleep -Seconds 2

$proc = Get-Process $AppName -ErrorAction SilentlyContinue
if (-not $proc) {
    throw "$AppName did not stay running. Check the log: $env:APPDATA\$AppName\log.txt"
}

Write-Host ''
Write-Host "Done. $AppName is running (pid $($proc.Id)); look for the tray icon." -ForegroundColor Green
Write-Host "  green = PAC script on, red = off. Left-click toggles, right-click opens the menu."
Write-Host "  config and log: $env:APPDATA\$AppName"
