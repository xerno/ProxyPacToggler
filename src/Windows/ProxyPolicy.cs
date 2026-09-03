using System.Security;
using Microsoft.Win32;

namespace ProxyPacToggler.Windows
{
    // Only explains a failed write; the detection is a hint, not a guarantee.
    internal static class ProxyPolicy
    {
        private const string PolicyPath =
            @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings";

        public static string ManagedHint()
        {
            return IsPerMachineProxyEnforced()
                ? " Group policy appears to manage proxy settings for this machine."
                : "";
        }

        // Called while already reporting a failure, so it must not add one of its own.
        private static bool IsPerMachineProxyEnforced()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PolicyPath, false))
                {
                    if (key == null) return false;
                    object value = key.GetValue("ProxySettingsPerUser");
                    return value is int && (int)value == 0;
                }
            }
            catch (SecurityException) { return false; }
        }
    }
}
