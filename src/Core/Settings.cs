namespace ProxyPacToggler.Core
{
    internal sealed class Settings
    {
        // Alphabetical; any one matching is enough, so the order is cosmetic.
        public const string DefaultAdapterMatch =
            "AnyConnect, Check Point, FortiClient, GlobalProtect, Netbird, OpenVPN, "
            + "Pulse Secure, SoftEther, SonicWall, Tailscale, TAP-Windows, Twingate, "
            + "VPN, WireGuard, ZeroTier";

        public Settings()
        {
            PacUrl = "";
            FollowVpn = true;
            Inverted = false;
            AdapterMatch = DefaultAdapterMatch;
        }

        public string PacUrl { get; set; }

        public bool FollowVpn { get; set; }

        // PAC on when the VPN is down, for setups wired the other way round.
        public bool Inverted { get; set; }

        public string AdapterMatch { get; set; }

        public Settings Copy()
        {
            Settings copy = new Settings();
            copy.PacUrl = PacUrl;
            copy.FollowVpn = FollowVpn;
            copy.Inverted = Inverted;
            copy.AdapterMatch = AdapterMatch;
            return copy;
        }
    }
}
