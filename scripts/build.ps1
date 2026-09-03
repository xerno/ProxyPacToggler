# Builds ProxyPacToggler.exe with the C# compiler that ships inside Windows.
#   build.ps1 -Release -Sign -PfxPath cert.pfx -PfxPassword hunter2
[CmdletBinding()]
param(
    [switch]$Release,
    [string]$OutDir,
    [switch]$Sign,
    [string]$PfxPath,
    [string]$PfxPassword,
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

# This script lives in scripts/; the repository root is one level up.
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$root = Split-Path -Parent $here
. (Join-Path $here 'build-config.ps1')

if (-not $OutDir) { $OutDir = Join-Path $root 'bin' }
$exe  = Join-Path $OutDir "$AppName.exe"
$icon = Join-Path $root 'src\app.ico'

# --- 1. Generated sources -----------------------------------------------------
& (Join-Path $here 'generate-build-info.ps1')

# --- 2. Prerequisites ---------------------------------------------------------
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw 'csc.exe not found. The .NET Framework 4.x compiler is present on Windows 8 and newer by default.'
}
Write-Host "Compiler: $csc"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# --- 3. Compile ---------------------------------------------------------------
# /target:winexe -> GUI subsystem, so no console window flashes at startup.
if ($Release) {
    Write-Host "Compiling $AppName $Version (Release)..."
    $optFlags = @('/optimize+', '/debug-')
} else {
    Write-Host "Compiling $AppName $Version (Debug)..."
    $optFlags = @('/optimize-', '/debug+', '/define:DEBUG')
}

$sources = Get-ChildItem (Join-Path $root 'src') -Filter *.cs -Recurse | ForEach-Object { $_.FullName }

$cscArgs = @('/nologo', '/target:winexe', '/platform:anycpu', '/warn:4', '/warnaserror+',
            "/out:$exe") + $optFlags
$cscArgs += ($References | ForEach-Object { "/reference:$_" })
if (Test-Path $icon) { $cscArgs += "/win32icon:$icon" }
$cscArgs += $sources

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }

# --- 4. Optional code signing -------------------------------------------------
if ($Sign) {
    if (-not $PfxPath) { throw '-Sign requires -PfxPath.' }
    if (-not (Test-Path $PfxPath)) { throw "PFX not found: $PfxPath" }

    $signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending | Select-Object -First 1
    if ($signtool) {
        $stArgs = @('sign', '/fd', 'SHA256', '/f', $PfxPath)
        if ($PfxPassword) { $stArgs += @('/p', $PfxPassword) }
        $stArgs += @('/tr', $TimestampUrl, '/td', 'SHA256', $exe)
        & $signtool.FullName @stArgs
        if ($LASTEXITCODE -ne 0) { throw "signtool failed with exit code $LASTEXITCODE" }
    } else {
        Write-Host 'signtool.exe not found, falling back to Set-AuthenticodeSignature.'
        $pw = $null
        if ($PfxPassword) { $pw = ConvertTo-SecureString $PfxPassword -AsPlainText -Force }
        if ($pw) {
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $PfxPath, $pw
        } else {
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $PfxPath
        }
        $r = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer $TimestampUrl
        if ($r.Status -ne 'Valid') { throw "Signing failed: $($r.Status) $($r.StatusMessage)" }
    }
    Write-Host 'Signed.' -ForegroundColor Green
}

# --- 5. Checksum --------------------------------------------------------------
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()
"$hash  $AppName.exe" | Set-Content (Join-Path $OutDir 'SHA256SUMS.txt') -Encoding ascii

$info = Get-Item $exe
Write-Host ("Built: {0} ({1:N0} bytes)" -f $exe, $info.Length) -ForegroundColor Green
Write-Host "SHA256: $hash"
