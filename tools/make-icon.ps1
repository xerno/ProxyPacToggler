# Generates src/app.ico. Draws it with System.Drawing and writes the .ico
# container by hand, so no image tool is needed. Re-run only to change artwork.
[CmdletBinding()]
param([string]$OutFile)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $OutFile) { $OutFile = Join-Path (Split-Path -Parent $root) 'src\app.ico' }

$sizes = 16, 32, 48, 64, 128, 256

function New-Png([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = 'AntiAlias'
        $g.TextRenderingHint = 'AntiAliasGridFit'
        $g.Clear([System.Drawing.Color]::Transparent)

        $pad = [math]::Max(1, [int]($size * 0.08))
        $d = $size - 2 * $pad
        $rect = New-Object System.Drawing.Rectangle $pad, $pad, $d, $d

        # Neutral blue-grey disc: the tray icon recolours itself at runtime,
        # this is just the file/shell icon.
        $c1 = [System.Drawing.Color]::FromArgb(255, 60, 110, 170)
        $c2 = [System.Drawing.Color]::FromArgb(255, 30, 60, 100)
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 45.0
        $g.FillEllipse($brush, $rect)
        $brush.Dispose()

        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 255, 255, 255)), ([float][math]::Max(1, $size / 24))
        $g.DrawEllipse($pen, $rect)
        $pen.Dispose()

        $font = New-Object System.Drawing.Font 'Segoe UI', ([float]($size * 0.52)), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $sf = New-Object System.Drawing.StringFormat
        $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
        $textRect = New-Object System.Drawing.RectangleF $pad, ($pad + $size * 0.02), $d, $d
        $g.DrawString('P', $font, [System.Drawing.Brushes]::White, $textRect, $sf)
        $font.Dispose(); $sf.Dispose()
    } finally { $g.Dispose() }

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    # A leading comma stops PowerShell from unrolling byte[] into Object[].
    return ,$ms.ToArray()
}

# --- assemble the .ico container ---------------------------------------------
# ICONDIR (6 bytes) + ICONDIRENTRY (16 bytes each) + the PNG payloads.
$pngs = @{}   # size -> byte[]
foreach ($s in $sizes) { $pngs[$s] = New-Png $s }

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
try {
    $bw.Write([uint16]0)               # reserved
    $bw.Write([uint16]1)               # type: icon
    $bw.Write([uint16]$sizes.Count)

    $offset = 6 + 16 * $sizes.Count
    foreach ($s in $sizes) {
        [byte[]]$bytes = $pngs[$s]
        $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))   # 0 means 256
        $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
        $bw.Write([byte]0)             # palette colours
        $bw.Write([byte]0)             # reserved
        $bw.Write([uint16]1)           # colour planes
        $bw.Write([uint16]32)          # bits per pixel
        $bw.Write([uint32]$bytes.Length)
        $bw.Write([uint32]$offset)
        $offset += $bytes.Length
    }
    foreach ($s in $sizes) { $bw.Write([byte[]]$pngs[$s]) }
    $bw.Flush()

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
    [System.IO.File]::WriteAllBytes($OutFile, $ms.ToArray())
} finally { $bw.Dispose() }

$info = Get-Item $OutFile
Write-Host ("Wrote {0} ({1:N0} bytes, sizes: {2})" -f $info.FullName, $info.Length, ($sizes -join ', ')) -ForegroundColor Green
