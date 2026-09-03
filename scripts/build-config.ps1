# build-config.ps1 - single source of truth for build settings.
#
# Dot-sourced by: build.ps1, test.ps1, install.ps1
# Keep VERSION in sync with the git tag you release.

$AppName          = 'ProxyPacToggler'
$Version          = '1.0'
$TestRunnerClass  = 'ProxyPacToggler.Tests.TestRunner'   # /main: entry point for tests
$InstallDir       = Join-Path $env:LOCALAPPDATA "Programs\$AppName"

# Framework assemblies the app links against.
$References = @(
    'System.dll'
    'System.Core.dll'
    'System.Drawing.dll'
    'System.Windows.Forms.dll'
)
