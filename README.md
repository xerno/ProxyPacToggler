# ProxyPacToggler

Switches the Windows proxy PAC script on and off with your VPN.

If your VPN needs a PAC script and everything else breaks while it is on, this saves
you the trip into Windows settings twice a day.

- Green tray icon: PAC script on. Red: off. Amber badge: something went wrong.
- VPN connects, the PAC script goes on. VPN drops, it goes off.
- Left-click toggles by hand, right-click opens the menu.
- One 76 kB exe. No installer, no admin rights, no Windows service.
- Touches only `HKCU` and `%APPDATA%\ProxyPacToggler`.

## Install

Build it yourself — a few seconds, nothing to install, and no SmartScreen warning
because the exe never came from the internet:

```
git clone https://github.com/<owner>/ProxyPacToggler
cd ProxyPacToggler
install.cmd
```

That tests, builds, installs into `%LOCALAPPDATA%\Programs\ProxyPacToggler`, starts it
and registers it to run at logon. All under your own user, so no admin prompt.

Or download `ProxyPacToggler.exe` from [Releases](../../releases) and run it. Windows
shows a SmartScreen warning for any unsigned download (**More info → Run anyway**);
the project is not code-signed. You can check `SHA256SUMS.txt`, or verify the binary
came out of this repository's CI:

```
gh attestation verify ProxyPacToggler.exe --repo <owner>/ProxyPacToggler
```

The first run asks for your PAC URL, or set it up-front with
`ProxyPacToggler.exe --url http://pac.example.com/proxy.pac`.

## Command line

| | |
|---|---|
| `--status` | PAC state, VPN state, configuration |
| `--on` `--off` `--toggle` | switch the PAC script |
| `--url <URL>` | store the PAC URL (`http`, `https` or `file`) |
| `--adapter <text>` | match the VPN adapter by name or description |
| `--list` | list adapters, to find the right name |
| `--install` `--uninstall` | run at logon, or stop doing that |
| `--version` `--help` | |

Exit codes: 0 success, 1 the operation failed, 2 wrong usage.

This is a GUI binary, so your shell does not wait for it. To capture output:
`Start-Process .\ProxyPacToggler.exe -ArgumentList '--status' -Wait -NoNewWindow`.

## Other setups

Detection matches a comma-separated list of substrings against each adapter's name
and description, and the default list covers the common VPN clients. If yours is not
recognised, find it with `--list` and set it: `--adapter "Contoso VPN"`.

If you need the PAC script on when the VPN is *off*, tick *Invert* in the menu.

## Configuration

`%APPDATA%\ProxyPacToggler\config.ini`, safe to edit by hand:

```ini
PacUrl=http://pac.example.com/proxy.pac
Auto=1                       # 0 = manual only, never follow the VPN
Invert=0                     # 1 = PAC on when the VPN is down
AdapterMatch=VPN, WireGuard, OpenVPN   # substrings, any one matching is enough
```

`log.txt` sits next to it and records every decision with the adapter state behind
it. *Open log* in the menu.

## Reliability

Once this works you stop thinking about it, so dying quietly is the worst failure: the
PAC script stays on, the VPN drops, browsing breaks for no visible reason.

So it runs as two processes that restart each other, the tray and a watcher (the same
binary with `--watch`). Each holds a named kernel object, which vanishes when a process
dies however it dies, force kill included, and the survivor notices within a minute. A
scheduled task would be the usual answer, but registering one needs admin rights.

*Exit* stops both for good and offers to switch the PAC script off first, since nothing
is left to do it afterwards. A kill counts as a crash and is undone.

If anything else changes `AutoConfigURL`, the tray sets it back within a minute and says
so. Your own manual toggle stands until the VPN changes.

Problems appear as a Windows notification, an amber badge until acknowledged, and a
*Last problem…* menu entry, throttled to one notification per ten minutes.

## How it works

The PAC script is one registry value, `AutoConfigURL` under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings`; writing it is what
the *Use setup script* switch does. The tool then calls `InternetSetOption` so running
browsers notice, and reads the value back, because a write can be accepted and silently
ignored — typically under group policy. Chromium-based browsers follow the system
setting, Firefox only if configured to.

URLs are validated first: absolute, `http`/`https`/`file`, with a host and no control
characters. A PAC script decides where every request goes, so this is a security
boundary, and it applies equally to the command line, the dialog and the config file.

VPN state comes from `NetworkChange` events rather than polling, marshalled off the
thread-pool thread onto the UI thread. A 1.5 s debounce absorbs the burst a client fires
while connecting, a 60 s re-check covers a missed event, and resuming from sleep is
another trigger. *Up* alone is not enough — clients raise the adapter before the tunnel
has an address — so a routable address is required.

## Development

The C# compiler ships inside Windows, so a clone builds with no SDK, no Visual Studio
and no NuGet.

| | |
|---|---|
| `build.cmd` | compile `src\` into `bin\` |
| `test.cmd` | compile `src\` + `tests\` into a test binary and run it |
| `install.cmd` | test, build, install, launch |

`install` builds nothing if the tests fail. Options are passed through:
`-Release`, `-Sign -PfxPath x.pfx`, `-SkipTests`, `-NoAutoStart`, `-KeepBinary`.

The `.cmd` files are entry points because batch is exempt from the PowerShell
execution policy, which blocks `.ps1` files on a stock Windows install. The logic is
in `scripts\*.ps1`, with shared settings in `scripts\build-config.ps1` and a generated
`BuildInfo.cs` behind `--version`. `tools\make-icon.ps1` regenerates `src\app.ico`.

```
src/Core/       the rule and the contracts, no OS dependencies
src/Windows/    registry, adapters, settings file, log, watcher, power hooks
src/Ui/         tray icon, menu, icons, tooltip, problem reporting
src/Cli/        argument dispatch and exit codes
tests/          one file per area, fakes in tests/Support
```

`ProxyController` decides when the PAC script is on and remembers what it last wrote,
so it can tell its own work from someone else's. No mutable static state, no file over
200 lines.

Tests compile *with* the real sources into one binary, so they exercise the shipping
code; there is no test framework, which would contradict needing nothing installed.
Policy runs against fakes, the Windows layer against a scratch registry key and a temp
directory — never the machine's own proxy configuration.

Builds are not reproducible (the compiler stamps a fresh module id), so compare
checksums against a release's `SHA256SUMS.txt`, not your own build. CI tests, builds,
smoke-tests the binary and publishes a rolling `latest` prerelease with a provenance
attestation.

## License

MIT
