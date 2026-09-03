# ProxyPacToggler

Switches the Windows proxy PAC script on and off with your VPN, from the tray.

Green icon: PAC on. Red: off. Amber badge: a problem. Left-click toggles, right-click
opens the menu. One 79 kB exe, no admin rights, touches only `HKCU` and
`%APPDATA%\ProxyPacToggler`.

## Install

```
git clone https://github.com/xerno/ProxyPacToggler
cd ProxyPacToggler
install.cmd
```

Tests, builds, installs into `%LOCALAPPDATA%\Programs\ProxyPacToggler` and runs at
logon. The C# compiler ships inside Windows, so there is no SDK to install and no
SmartScreen warning. Or take the exe from [Releases](../../releases) and accept the
warning Windows shows for any unsigned download (**More info → Run anyway**).

The first run asks for your PAC URL. If your VPN client is not one of the 15 matched by
default, `--list` shows the adapters and `--adapter "Contoso VPN"` picks one.

## Command line

| | |
|---|---|
| `--status` | PAC state, VPN state, configuration |
| `--on` `--off` `--toggle` | switch the PAC script |
| `--url <URL>` | store the PAC URL (`http`, `https` or `file`) |
| `--adapter <text>` | match the VPN adapter by name or description |
| `--list` | list adapters |
| `--install` `--uninstall` | run at logon, or stop |
| `--version` `--help` | |

Exit codes: 0 ok, 1 failed, 2 wrong usage. It is a GUI binary, so the shell does not
wait for it: `Start-Process .\ProxyPacToggler.exe -ArgumentList '--status' -Wait`.

## Behaviour

- Detection is event-driven, debounced 1.5 s, re-checked every 60 s. An adapter counts
  as connected only once it has a routable address.
- The registry write is read back, because group policy can accept it and then ignore it.
- Anything else changing `AutoConfigURL` is set back within a minute and reported.
- Two processes, the tray and `--watch`, restart each other, so a crash or a force kill
  recovers without the admin rights a scheduled task would need. *Exit* stops both and
  offers to switch the PAC script off first.
- PAC URLs must be absolute `http`, `https` or `file` with a host, wherever they come
  from — a PAC script decides where every request goes.
- `config.ini` is hand-editable and `log.txt` records every decision, both in
  `%APPDATA%\ProxyPacToggler`. *Invert* in the menu is for setups wired the other way.

## Development

| | |
|---|---|
| `build.cmd` | compile `src\` into `bin\` |
| `test.cmd` | compile `src\` + `tests\` and run the tests |
| `install.cmd` | test, build, install, launch |

Red tests build nothing. The `.cmd` files exist because batch is exempt from the
PowerShell execution policy that blocks `.ps1` on a stock Windows; the logic is in
`scripts\*.ps1`. Tests compile with the real sources into one binary and run against
fakes and a scratch registry key, never your own proxy configuration.

## License

MIT
