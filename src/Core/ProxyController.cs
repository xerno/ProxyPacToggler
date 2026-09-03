using System;

namespace ProxyPacToggler.Core
{
    // Owns when the PAC script is on, knowing nothing of the registry or the tray.
    internal sealed class ProxyController
    {
        private readonly IProxySettings proxy;
        private readonly IVpnMonitor vpn;
        private readonly ISettingsStore store;
        private readonly ILog log;

        private bool? wasVpnUp;
        private VpnStatus lastVpn;
        private bool synchronising;

        // What this app last wrote; anything else came from outside and gets taken back.
        private string appliedPacUrl;
        private bool manualOverride;

        public ProxyController(IProxySettings proxy, IVpnMonitor vpn, ISettingsStore store, ILog log)
        {
            if (proxy == null) throw new ArgumentNullException("proxy");
            if (vpn == null) throw new ArgumentNullException("vpn");
            if (store == null) throw new ArgumentNullException("store");
            if (log == null) throw new ArgumentNullException("log");

            this.proxy = proxy;
            this.vpn = vpn;
            this.store = store;
            this.log = log;

            Settings = store.Load();
        }

        public Settings Settings { get; private set; }

        public bool IsPacEnabled { get { return proxy.IsPacEnabled; } }

        public string CurrentPacUrl { get { return proxy.CurrentPacUrl; } }

        // Reused rather than re-read, because enumerating adapters is not free.
        public VpnStatus LastVpn { get { return lastVpn ?? ReadVpn(); } }

        public VpnStatus ReadVpn()
        {
            lastVpn = vpn.Read(Settings.AdapterMatch);
            return lastVpn;
        }

        public SyncOutcome Synchronise(bool force, string trigger)
        {
            if (synchronising)
                return SyncOutcome.Of(Result.Fail("a synchronisation is already in progress"),
                                      null, false);

            synchronising = true;
            try
            {
                VpnStatus status = ReadVpn();
                bool vpnChanged = !wasVpnUp.HasValue || wasVpnUp.Value != status.IsUp;
                wasVpnUp = status.IsUp;

                if (vpnChanged)
                {
                    manualOverride = false;
                    log.Write("[" + trigger + "] VPN " + (status.IsUp ? "UP" : "down")
                              + " (" + status.Detail + ")");
                }

                string drift = DetectExternalChange(WantedPacUrl(status));

                if (!Settings.FollowVpn || !(force || vpnChanged || drift != null))
                    return SyncOutcome.Of(Result.Ok(), drift, !status.MatchedAny);

                Result result = PacDecision.WantsPac(status.IsUp, Settings.Inverted)
                    ? Apply(Settings.PacUrl)
                    : Clear();

                return SyncOutcome.Of(result, drift, !status.MatchedAny);
            }
            finally
            {
                synchronising = false;
            }
        }

        // Null in manual mode, where the VPN state says nothing about what is wanted.
        private string WantedPacUrl(VpnStatus status)
        {
            if (!Settings.FollowVpn) return null;
            return PacDecision.WantsPac(status.IsUp, Settings.Inverted) ? Settings.PacUrl : "";
        }

        private string DetectExternalChange(string wanted)
        {
            string actual = proxy.CurrentPacUrl;

            // Nothing to compare on the first run, and a manual choice is our own doing.
            if (appliedPacUrl == null || manualOverride || actual == appliedPacUrl) return null;

            // VPN clients clear the setting as they disconnect, which is where it was going.
            if (actual == wanted) return null;

            string change = "the proxy setting was changed outside ProxyPacToggler to \""
                            + (actual.Length == 0 ? "(off)" : actual) + "\"";
            log.Write(change + "; taking it back");
            return change;
        }

        public Result Enable(string pacUrl)
        {
            return Apply(pacUrl);
        }

        public Result Disable()
        {
            return Clear();
        }

        // A manual choice stands until the VPN state actually changes again.
        public Result Toggle()
        {
            Result result = proxy.IsPacEnabled ? Clear() : Apply(Settings.PacUrl);
            wasVpnUp = ReadVpn().IsUp;
            manualOverride = true;
            log.Write("manual toggle");
            return result;
        }

        private Result Apply(string pacUrl)
        {
            Result result = proxy.Apply(pacUrl);
            if (result.Succeeded) appliedPacUrl = proxy.CurrentPacUrl;
            return result;
        }

        private Result Clear()
        {
            Result result = proxy.Clear();
            if (result.Succeeded) appliedPacUrl = "";
            return result;
        }

        public Result UpdateSettings(Action<Settings> change)
        {
            Settings candidate = Settings.Copy();
            change(candidate);
            Settings = candidate;
            return store.Save(Settings);
        }

        public void ForgetVpnState()
        {
            wasVpnUp = null;
            manualOverride = false;
        }

        public string DescribeAdapters()
        {
            return vpn.DescribeAdapters();
        }

        public string SettingsLocation { get { return store.Location; } }

        public string LogLocation { get { return log.Location; } }
    }
}
