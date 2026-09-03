namespace ProxyPacToggler.Core
{
    internal static class PacDecision
    {
        public static bool WantsPac(bool vpnIsUp, bool inverted)
        {
            return inverted ? !vpnIsUp : vpnIsUp;
        }
    }
}
