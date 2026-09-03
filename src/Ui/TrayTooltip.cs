using System;

namespace ProxyPacToggler.Ui
{
    // NotifyIcon.Text throws above 63 characters and is set on every timer tick.
    internal static class TrayTooltip
    {
        public const int MaxLength = 63;

        private const string ProblemMarker = "! problem - see menu";

        public static string Compose(bool pacEnabled, bool followingVpn, bool vpnUp, bool hasProblem)
        {
            string text = (pacEnabled ? "PAC: ON" : "PAC: off")
                          + (followingVpn ? " (auto)" : " (manual)")
                          + Environment.NewLine
                          + (vpnUp ? "VPN: connected" : "VPN: disconnected");

            if (hasProblem) text += Environment.NewLine + ProblemMarker;

            return text.Length <= MaxLength ? text : text.Substring(0, MaxLength);
        }
    }
}
