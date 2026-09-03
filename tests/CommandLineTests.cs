using ProxyPacToggler.Cli;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Tests
{
    internal static class CommandLineTests
    {
        private const string Url = "http://pac.example.com/proxy.pac";

        public static void Run()
        {
            NormalisesArguments();
            ReportsStatus();
            SwitchesPac();
            RefusesUnsafeUrls();
            RefusesBadUsage();
            RefusesExtraArguments();
        }

        private static CommandLine Build(out FakeProxySettings proxy, out FakeVpnMonitor vpn,
                                         out RecordingConsole console, bool withUrl)
        {
            Settings initial = new Settings();
            if (withUrl) initial.PacUrl = Url;

            proxy = new FakeProxySettings();
            vpn = new FakeVpnMonitor();
            console = new RecordingConsole();
            FakeLog log = new FakeLog();

            ProxyController controller = new ProxyController(
                proxy, vpn, new FakeSettingsStore(initial), log);
            AutoStartEntry autoStart = new AutoStartEntry(
                "ProxyPacToggler.Tests.NotUsed", @"C:\nowhere\app.exe", log);

            return new CommandLine(controller, autoStart, console);
        }

        private static void NormalisesArguments()
        {
            Assert.Case("CommandLine.Normalize");
            Assert.Equal("status", CommandLine.Normalize("--status"), "double dash");
            Assert.Equal("status", CommandLine.Normalize("-status"), "single dash");
            Assert.Equal("status", CommandLine.Normalize("/status"), "slash, Windows style");
            Assert.Equal("status", CommandLine.Normalize("--STATUS"), "upper case");
            Assert.Equal("status", CommandLine.Normalize("status"), "a bare word");
            Assert.Equal("", CommandLine.Normalize(null), "a null argument");
        }

        private static void ReportsStatus()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            RecordingConsole console;
            CommandLine cli = Build(out proxy, out vpn, out console, true);

            Assert.Case("--status");
            vpn.IsUp = true;
            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--status" }), "exits successfully");
            Assert.True(console.AllOutput.Contains("VPN:      up"), "reports the VPN as up");
            Assert.True(console.AllOutput.Contains(Url), "reports the configured URL");
            Assert.Equal("", console.AllErrors, "writes nothing to stderr");
        }

        private static void SwitchesPac()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            RecordingConsole console;
            CommandLine cli = Build(out proxy, out vpn, out console, true);

            Assert.Case("--on, --off and --toggle");
            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--on" }), "--on succeeds");
            Assert.True(proxy.IsPacEnabled, "the PAC script is on");

            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--toggle" }), "--toggle succeeds");
            Assert.False(proxy.IsPacEnabled, "the PAC script is off again");

            Assert.Case("a failing switch exits non-zero");
            proxy.FailWith = "access denied";
            Assert.Equal(CommandLine.ExitFailed, cli.Run(new[] { "--on" }), "--on reports failure");
            Assert.True(console.AllErrors.Contains("access denied"), "and says why");
        }

        private static void RefusesUnsafeUrls()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            RecordingConsole console;
            CommandLine cli = Build(out proxy, out vpn, out console, false);

            Assert.Case("--url refuses an unsafe URL");
            Assert.Equal(CommandLine.ExitFailed, cli.Run(new[] { "--url", "javascript:alert(1)" }),
                         "a javascript URL is refused");
            Assert.True(console.AllErrors.Contains("javascript"), "and names the offending scheme");

            Assert.Case("--url accepts and stores a good URL");
            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--url", "  " + Url + "  " }),
                         "a padded URL is accepted");
            Assert.True(console.AllOutput.Contains(Url), "and confirmed");

            Assert.Case("--on without a configured URL is a usage error");
            CommandLine bare = Build(out proxy, out vpn, out console, false);
            Assert.Equal(CommandLine.ExitUsage, bare.Run(new[] { "--on" }), "exits with the usage code");
            Assert.True(console.AllErrors.Contains("--url"), "and points at --url");
        }

        private static void RefusesExtraArguments()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            RecordingConsole console;
            CommandLine cli = Build(out proxy, out vpn, out console, true);

            Assert.Case("unquoted values with spaces are called out, not silently truncated");
            Assert.Equal(CommandLine.ExitUsage,
                         cli.Run(new[] { "--adapter", "Contoso", "VPN" }),
                         "the extra word is a usage error");
            Assert.True(console.AllErrors.Contains("quoted"), "and the message explains quoting");

            cli.Run(new[] { "--status" });
            Assert.True(console.AllOutput.Contains("Adapter:  " + Settings.DefaultAdapterMatch),
                        "the adapter setting was left alone, not set to the first word");

            Assert.Case("extra arguments on a valueless option are refused");
            Assert.Equal(CommandLine.ExitUsage, cli.Run(new[] { "--status", "extra" }),
                         "--status takes no value");
            Assert.Equal(CommandLine.ExitUsage, cli.Run(new[] { "--url", "http://a/b.pac", "extra" }),
                         "--url takes exactly one value");
        }

        private static void RefusesBadUsage()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            RecordingConsole console;
            CommandLine cli = Build(out proxy, out vpn, out console, true);

            Assert.Case("bad usage is rejected with the usage exit code");
            Assert.Equal(CommandLine.ExitUsage, cli.Run(new[] { "--url" }), "--url without a value");
            Assert.Equal(CommandLine.ExitUsage, cli.Run(new[] { "--adapter" }), "--adapter without a value");
            Assert.Equal(CommandLine.ExitUsage, cli.Run(new[] { "--nonsense" }), "an unknown option");

            Assert.Case("an empty adapter pattern is refused");
            Assert.Equal(CommandLine.ExitFailed, cli.Run(new[] { "--adapter", "   " }),
                         "an empty pattern would match nothing");

            Assert.Case("--help and --version succeed");
            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--help" }), "--help");
            Assert.Equal(CommandLine.ExitOk, cli.Run(new[] { "--version" }), "--version");
            Assert.True(console.AllOutput.Contains("Exit codes"), "help documents the exit codes");
        }
    }
}
