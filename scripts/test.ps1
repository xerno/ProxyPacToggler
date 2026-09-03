# Compiles src/ together with tests/ into one binary and runs it. Nothing is
# installed and the machine's own proxy configuration is never touched.
[CmdletBinding()]
param([switch]$KeepBinary)

$ErrorActionPreference = 'Stop'

# This script lives in scripts/; the repository root is one level up.
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$root = Split-Path -Parent $here
. (Join-Path $here 'build-config.ps1')

# Built into TEMP: the binary is a throwaway, and a network share can deny execute.
$buildDir = Join-Path ([IO.Path]::GetTempPath()) "$AppName.build"
$testExe  = Join-Path $buildDir "$AppName.Tests.exe"

# --- generated sources --------------------------------------------------------
& (Join-Path $here 'generate-build-info.ps1')

# --- locate the compiler ------------------------------------------------------
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { throw 'csc.exe not found (.NET Framework 4.x is expected to be present).' }

New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

# --- compile app + tests into one test binary ---------------------------------
# /main: picks the test entry point over the app's own Main.
$sources = @(Get-ChildItem (Join-Path $root 'src') -Filter *.cs -Recurse | ForEach-Object { $_.FullName }) +
           @(Get-ChildItem (Join-Path $root 'tests') -Filter *.cs -Recurse | ForEach-Object { $_.FullName })

$cscArgs = @('/nologo', '/target:exe', '/platform:anycpu', '/warn:4', '/warnaserror+',
              "/main:$TestRunnerClass", "/out:$testExe")
$cscArgs += ($References | ForEach-Object { "/reference:$_" })
$cscArgs += $sources

Write-Host "Compiling $AppName tests..."
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed with exit code $LASTEXITCODE" }

# --- run ----------------------------------------------------------------------
Write-Host "Running $AppName tests..."
Write-Host ''
& $testExe
$status = $LASTEXITCODE

if ($KeepBinary) { Write-Host "Test binary kept: $testExe" }
else { Remove-Item $testExe -Force -ErrorAction SilentlyContinue }

if ($status -ne 0) {
    Write-Host ''
    Write-Host "==> tests failed (exit code $status)" -ForegroundColor Red
} else {
    Write-Host ''
    Write-Host '==> tests passed' -ForegroundColor Green
}
exit $status
