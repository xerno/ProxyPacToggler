using System;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Cli
{
    internal sealed class CommandLine
    {
        public const int ExitOk = 0;
        public const int ExitFailed = 1;
        public const int ExitUsage = 2;

        private readonly ProxyController controller;
        private readonly AutoStartEntry autoStart;
        private readonly IConsole console;

        public CommandLine(ProxyController controller, AutoStartEntry autoStart, IConsole console)
        {
            this.controller = controller;
            this.autoStart = autoStart;
            this.console = console;
        }

        public static string Normalize(string argument)
        {
            return argument == null ? "" : argument.TrimStart('-', '/').Trim().ToLowerInvariant();
        }

        public int Run(string[] args)
        {
            string command = Normalize(args[0]);
            int allowed = ExpectedArgumentCount(command);
            if (args.Length > allowed) return TooManyArguments(args, allowed);

            switch (command)
            {
                case "on":        return SwitchPac(true);
                case "off":       return SwitchPac(false);
                case "toggle":    return SwitchPac(!controller.IsPacEnabled);
                case "status":    return PrintStatus();
                case "url":       return StorePacUrl(args);
                case "adapter":   return StoreAdapterMatch(args);
                case "list":      return ListAdapters();
                case "install":   return SetAutoStart(true);
                case "uninstall": return SetAutoStart(false);

                case "v":
                case "version":   return PrintVersion();

                case "h":
                case "help":
                case "?":         console.Write(HelpText.Full); return ExitOk;

                default:
                    console.WriteError("Unknown option: " + args[0]);
                    console.Write(HelpText.Full);
                    return ExitUsage;
            }
        }

        private static int ExpectedArgumentCount(string command)
        {
            return command == "url" || command == "adapter" ? 2 : 1;
        }

        // An unquoted multi-word value would otherwise store only its first word.
        private int TooManyArguments(string[] args, int allowed)
        {
            console.WriteError("Too many arguments for " + args[0] + ": got "
                               + (args.Length - 1) + ", expected " + (allowed - 1) + ".");

            if (allowed == 2)
                console.WriteError("A value containing spaces has to be quoted, "
                                   + "for example: --adapter \"Contoso VPN\"");

            return ExitUsage;
        }

        private int SwitchPac(bool on)
        {
            if (!on) return Report(controller.Disable(), "PAC off");

            string url = controller.Settings.PacUrl.Trim();
            if (url.Length == 0)
            {
                console.WriteError("No PAC URL configured. Use: ProxyPacToggler.exe --url <URL>");
                return ExitUsage;
            }

            return Report(controller.Enable(url), "PAC on: " + url);
        }

        private int PrintStatus()
        {
            VpnStatus vpn = controller.ReadVpn();
            Settings settings = controller.Settings;

            console.Write("PAC:      " + (controller.IsPacEnabled
                ? "on (" + controller.CurrentPacUrl + ")" : "off"));
            console.Write("VPN:      " + (vpn.IsUp ? "up" : "down") + "  [" + vpn.Detail + "]");
            console.Write("Mode:     " + (settings.FollowVpn ? "auto" : "manual")
                          + (settings.Inverted ? ", inverted" : ""));
            console.Write("PAC URL:  " + (settings.PacUrl.Length > 0 ? settings.PacUrl : "(not set)"));
            console.Write("Adapter:  " + settings.AdapterMatch);
            console.Write("Settings: " + controller.SettingsLocation);
            console.Write("Log:      " + controller.LogLocation);
            return ExitOk;
        }

        private int StorePacUrl(string[] args)
        {
            if (args.Length < 2) return Usage("--url <URL>");

            string normalized;
            string error;
            if (!PacUrlValidator.TryNormalize(args[1], out normalized, out error))
            {
                console.WriteError("Refusing that PAC URL: " + error + ".");
                return ExitFailed;
            }

            Result saved = controller.UpdateSettings(delegate(Settings s) { s.PacUrl = normalized; });
            return Report(saved, "PAC URL saved: " + normalized);
        }

        private int StoreAdapterMatch(string[] args)
        {
            if (args.Length < 2) return Usage("--adapter <substring>");

            string match = args[1].Trim();
            if (match.Length == 0)
            {
                console.WriteError("An empty adapter pattern would never match. Nothing changed.");
                return ExitFailed;
            }

            Result saved = controller.UpdateSettings(delegate(Settings s) { s.AdapterMatch = match; });
            return Report(saved, "Adapter match saved: " + match);
        }

        private int ListAdapters()
        {
            console.Write("Network adapters:");
            console.WriteRaw(controller.DescribeAdapters());
            return ExitOk;
        }

        private int SetAutoStart(bool enabled)
        {
            return Report(autoStart.Set(enabled), enabled
                ? "Autostart enabled for the current user."
                : "Autostart disabled.");
        }

        private int PrintVersion()
        {
            console.Write("ProxyPacToggler " + BuildInfo.Version
                          + " (built " + BuildInfo.Date + ", commit " + BuildInfo.Commit + ")");
            return ExitOk;
        }

        private int Report(Result result, string successMessage)
        {
            if (result.Succeeded)
            {
                console.Write(successMessage);
                return ExitOk;
            }

            console.WriteError("Failed: " + result.Error + ".");
            return ExitFailed;
        }

        private int Usage(string form)
        {
            console.WriteError("Usage: ProxyPacToggler.exe " + form);
            return ExitUsage;
        }
    }
}
