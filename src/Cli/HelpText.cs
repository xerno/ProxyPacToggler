namespace ProxyPacToggler.Cli
{
    internal static class HelpText
    {
        public const string Full =
@"ProxyPacToggler - switches the Windows proxy PAC script on and off with your VPN.

With no arguments it starts the tray icon: green = PAC on, red = off.
Left-click toggles, right-click opens the menu.

  --status              PAC state, VPN state, configuration
  --on --off --toggle   switch the PAC script
  --url <URL>           store the PAC URL (http, https or file)
  --adapter <text>      match the VPN adapter by name or description
  --list                list adapters, to find the right name
  --install             run at logon for the current user
  --uninstall           stop running at logon
  --version --help

Exit codes: 0 success, 1 failed, 2 wrong usage.
Settings and log: %APPDATA%\ProxyPacToggler. Only HKCU is touched, no admin needed.";
    }
}
