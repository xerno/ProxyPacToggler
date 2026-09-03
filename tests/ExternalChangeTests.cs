using ProxyPacToggler.Core;

namespace ProxyPacToggler.Tests
{
    // The tray is the authority: interference gets taken back and reported.
    internal static class ExternalChangeTests
    {
        private const string Url = "http://pac.example.com/proxy.pac";
        private const string Foreign = "http://evil.example.com/other.pac";

        public static void Run()
        {
            TakesTheSettingBack();
            SaysNothingWhenTheVpnClientBeatsUsToIt();
            SaysNothingOnTheFirstRun();
            IgnoresItsOwnManualChoice();
            ReportsAMissingAdapter();
        }

        private static ProxyController Build(out FakeProxySettings proxy, out FakeVpnMonitor vpn,
                                             out FakeLog log)
        {
            Settings initial = new Settings();
            initial.PacUrl = Url;

            proxy = new FakeProxySettings();
            vpn = new FakeVpnMonitor();
            log = new FakeLog();
            return new ProxyController(proxy, vpn, new FakeSettingsStore(initial), log);
        }

        private static void TakesTheSettingBack()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out log);

            Assert.Case("a foreign value while the VPN is up is taken back");
            vpn.IsUp = true;
            controller.Synchronise(true, "start");
            Assert.Equal(Url, proxy.CurrentPacUrl, "our own URL is in place");

            proxy.ChangeExternally(Foreign);
            SyncOutcome outcome = controller.Synchronise(false, "safety");

            Assert.True(outcome.ExternalChange != null, "the change is reported");
            Assert.True(outcome.ExternalChange.Contains(Foreign), "and names the foreign value");
            Assert.Equal(Url, proxy.CurrentPacUrl, "and the setting is back to ours");

            Assert.Case("being switched off from outside while the VPN is up is taken back");
            proxy.ChangeExternally("");
            outcome = controller.Synchronise(false, "safety");
            Assert.True(outcome.ExternalChange != null, "the change is reported");
            Assert.Equal(Url, proxy.CurrentPacUrl, "the PAC script is on again");

            Assert.Case("a settled state reports nothing");
            outcome = controller.Synchronise(false, "safety");
            Assert.True(outcome.ExternalChange == null, "no news when nothing changed");
        }

        private static void SaysNothingWhenTheVpnClientBeatsUsToIt()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out log);

            Assert.Case("a client clearing the setting as it disconnects is not interference");
            vpn.IsUp = true;
            controller.Synchronise(true, "start");

            proxy.ChangeExternally("");
            vpn.IsUp = false;
            SyncOutcome outcome = controller.Synchronise(false, "event");

            Assert.True(outcome.ExternalChange == null, "the value was going off anyway");
            Assert.False(proxy.IsPacEnabled, "and it stays off");

            Assert.Case("the same value while the VPN is still up is interference");
            vpn.IsUp = true;
            controller.Synchronise(true, "event");
            proxy.ChangeExternally("");
            outcome = controller.Synchronise(false, "safety");
            Assert.True(outcome.ExternalChange != null, "now it contradicts the VPN state");
        }

        private static void SaysNothingOnTheFirstRun()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out log);

            Assert.Case("a value already present at startup is not called foreign");
            proxy.ChangeExternally("http://pac.example.com/left-over.pac");
            vpn.IsUp = true;

            SyncOutcome outcome = controller.Synchronise(true, "start");
            Assert.True(outcome.ExternalChange == null,
                        "nothing to compare against yet, so nothing is reported");
            Assert.Equal(Url, proxy.CurrentPacUrl, "but the correct URL is applied anyway");
        }

        private static void IgnoresItsOwnManualChoice()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out log);

            Assert.Case("the app's own manual toggle is not mistaken for interference");
            vpn.IsUp = true;
            controller.Synchronise(true, "start");
            controller.Toggle();

            SyncOutcome outcome = controller.Synchronise(false, "safety");
            Assert.True(outcome.ExternalChange == null, "the manual choice is not reported");
            Assert.False(proxy.IsPacEnabled, "and it is not undone either");
        }

        private static void ReportsAMissingAdapter()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out log);

            Assert.Case("a pattern matching no adapter is flagged");
            vpn.MatchesAnAdapter = false;
            SyncOutcome outcome = controller.Synchronise(true, "start");
            Assert.True(outcome.AdapterMissing, "the caller is told the pattern matches nothing");

            Assert.Case("a pattern matching an adapter is not flagged");
            vpn.MatchesAnAdapter = true;
            outcome = controller.Synchronise(true, "start");
            Assert.False(outcome.AdapterMissing, "nothing to flag");
        }
    }
}
