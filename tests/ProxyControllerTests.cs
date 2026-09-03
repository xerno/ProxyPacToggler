using ProxyPacToggler.Core;

namespace ProxyPacToggler.Tests
{
    internal static class ProxyControllerTests
    {
        private const string Url = "http://pac.example.com/proxy.pac";

        public static void Run()
        {
            FollowsTheVpn();
            RespectsInverted();
            RespectsManualMode();
            ActsOnlyOnChange();
            ReportsFailures();
            RemembersManualChoice();
            RefusesToRunTwiceAtOnce();
            PersistsSettings();
            ReadsTheAdaptersOncePerSync();
        }

        private static ProxyController Build(out FakeProxySettings proxy, out FakeVpnMonitor vpn,
                                             out FakeSettingsStore store, out FakeLog log)
        {
            Settings initial = new Settings();
            initial.PacUrl = Url;

            proxy = new FakeProxySettings();
            vpn = new FakeVpnMonitor();
            store = new FakeSettingsStore(initial);
            log = new FakeLog();
            return new ProxyController(proxy, vpn, store, log);
        }

        private static void FollowsTheVpn()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("the PAC script follows the VPN");
            vpn.IsUp = true;
            Assert.True(controller.Synchronise(false, "test").Result.Succeeded, "connecting succeeds");
            Assert.True(proxy.IsPacEnabled, "PAC is on while the VPN is up");
            Assert.Equal(Url, proxy.CurrentPacUrl, "the configured URL was applied");

            vpn.IsUp = false;
            Assert.True(controller.Synchronise(false, "test").Result.Succeeded, "disconnecting succeeds");
            Assert.False(proxy.IsPacEnabled, "PAC is off while the VPN is down");
        }

        private static void RespectsInverted()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("inverted mode reverses the rule");
            controller.UpdateSettings(delegate(Settings s) { s.Inverted = true; });

            vpn.IsUp = false;
            controller.Synchronise(true, "test");
            Assert.True(proxy.IsPacEnabled, "PAC is on while the VPN is down");

            vpn.IsUp = true;
            controller.Synchronise(false, "test");
            Assert.False(proxy.IsPacEnabled, "PAC is off while the VPN is up");
        }

        private static void RespectsManualMode()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("manual mode never touches the PAC setting on its own");
            controller.UpdateSettings(delegate(Settings s) { s.FollowVpn = false; });

            vpn.IsUp = true;
            controller.Synchronise(true, "test");
            Assert.Equal(0, proxy.ApplyCount, "nothing was applied");
            Assert.Equal(0, proxy.ClearCount, "nothing was cleared");
            Assert.False(proxy.IsPacEnabled, "the PAC setting is untouched");
        }

        private static void ActsOnlyOnChange()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("an unchanged VPN state causes no work");
            vpn.IsUp = true;
            controller.Synchronise(false, "first");
            int appliesAfterFirst = proxy.ApplyCount;

            controller.Synchronise(false, "second");
            Assert.Equal(appliesAfterFirst, proxy.ApplyCount, "the second run applied nothing");

            controller.Synchronise(true, "forced");
            Assert.True(proxy.ApplyCount > appliesAfterFirst, "but a forced run does act");
        }

        private static void ReportsFailures()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("a failing proxy layer is reported, not swallowed");
            proxy.FailWith = "the setting did not stick";
            vpn.IsUp = true;

            SyncOutcome outcome = controller.Synchronise(true, "test");
            Assert.False(outcome.Result.Succeeded, "the failure surfaces");
            Assert.Equal("the setting did not stick", outcome.Result.Error,
                         "with the underlying reason");
        }

        private static void RemembersManualChoice()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("a manual toggle survives until the VPN state changes");
            vpn.IsUp = true;
            controller.Synchronise(true, "test");
            Assert.True(proxy.IsPacEnabled, "PAC starts on");

            Assert.True(controller.Toggle().Succeeded, "the manual toggle succeeds");
            Assert.False(proxy.IsPacEnabled, "PAC is now off by hand");

            controller.Synchronise(false, "unchanged");
            Assert.False(proxy.IsPacEnabled, "an unchanged VPN does not undo the manual choice");

            vpn.IsUp = false;
            controller.Synchronise(false, "changed");
            vpn.IsUp = true;
            controller.Synchronise(false, "changed back");
            Assert.True(proxy.IsPacEnabled, "a real VPN change takes over again");
        }

        private static void RefusesToRunTwiceAtOnce()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("re-entrant synchronisation is refused");
            Assert.True(controller.Synchronise(true, "outer").Result.Succeeded, "a lone run succeeds");
            Assert.True(controller.Synchronise(true, "again").Result.Succeeded,
                        "and the guard is released afterwards");
        }

        private static void ReadsTheAdaptersOncePerSync()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("enumerating adapters happens once per synchronisation");
            controller.Synchronise(true, "start");
            Assert.Equal(1, vpn.ReadCount, "one read");

            Assert.True(controller.LastVpn != null, "the reading is available afterwards");
            Assert.Equal(1, vpn.ReadCount, "and re-reading it costs nothing");
        }

        private static void PersistsSettings()
        {
            FakeProxySettings proxy;
            FakeVpnMonitor vpn;
            FakeSettingsStore store;
            FakeLog log;
            ProxyController controller = Build(out proxy, out vpn, out store, out log);

            Assert.Case("settings changes are persisted");
            Assert.True(controller.UpdateSettings(
                delegate(Settings s) { s.AdapterMatch = "WireGuard"; }).Succeeded, "the save succeeds");
            Assert.Equal("WireGuard", controller.Settings.AdapterMatch, "the new value is in effect");
            Assert.Equal(1, store.SaveCount, "the store was asked to save once");

            controller.ReadVpn();
            Assert.Equal("WireGuard", vpn.LastPatternAsked, "detection uses the new pattern");

            Assert.Case("a failing save is reported but the change still applies");
            store.FailWith = "disk full";
            Result saved = controller.UpdateSettings(delegate(Settings s) { s.Inverted = true; });
            Assert.False(saved.Succeeded, "the failure surfaces");
            Assert.True(controller.Settings.Inverted, "the in-memory change is still in effect");
        }
    }
}
