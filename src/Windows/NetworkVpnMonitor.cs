using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    internal sealed class NetworkVpnMonitor : IVpnMonitor, IDisposable
    {
        public event EventHandler Changed;

        public NetworkVpnMonitor()
        {
            NetworkChange.NetworkAddressChanged += OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
        }

        public VpnStatus Read(string adapterMatch)
        {
            string[] wanted = Split(adapterMatch);
            List<string> matched = new List<string>();
            bool isUp = false;

            try
            {
                foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!Matches(adapter.Name, adapter.Description, wanted)) continue;

                    bool operational = adapter.OperationalStatus == OperationalStatus.Up;
                    bool addressed = operational && HasRoutableAddress(adapter);

                    matched.Add(adapter.Name + "=" + adapter.OperationalStatus
                                + (operational && !addressed ? "(no address yet)" : ""));
                    if (addressed) isUp = true;
                }
            }
            catch (NetworkInformationException ex)
            {
                return new VpnStatus(false, true, "adapter enumeration failed: " + ex.Message);
            }

            bool matchedAny = matched.Count > 0;
            string detail = matchedAny
                ? string.Join(", ", matched.ToArray())
                : "no adapter matched " + Describe(wanted);
            return new VpnStatus(isUp, matchedAny, detail);
        }

        // Quoting one pattern is helpful; quoting the whole default list is noise.
        public static string Describe(string patterns)
        {
            return Describe(Split(patterns));
        }

        private static string Describe(string[] wanted)
        {
            return wanted.Length == 1
                ? "\"" + wanted[0] + "\""
                : "any of " + wanted.Length + " configured patterns";
        }

        private static string[] Split(string patterns)
        {
            if (string.IsNullOrEmpty(patterns)) return new string[0];

            List<string> wanted = new List<string>();
            foreach (string pattern in patterns.Split(','))
            {
                string trimmed = pattern.Trim();
                if (trimmed.Length > 0) wanted.Add(trimmed);
            }
            return wanted.ToArray();
        }

        // A list, so one default covers the common clients without singling any out.
        public static bool Matches(string name, string description, string patterns)
        {
            return Matches(name, description, Split(patterns));
        }

        // Split once per read rather than once per adapter.
        private static bool Matches(string name, string description, string[] patterns)
        {
            foreach (string wanted in patterns)
            {
                if (Contains(name, wanted) || Contains(description, wanted)) return true;
            }
            return false;
        }

        private static bool Contains(string haystack, string needle)
        {
            return haystack != null
                   && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Clients raise the adapter before the tunnel has a usable address.
        private static bool HasRoutableAddress(NetworkInterface adapter)
        {
            List<IPAddress> addresses = new List<IPAddress>();
            foreach (UnicastIPAddressInformation each in adapter.GetIPProperties().UnicastAddresses)
            {
                addresses.Add(each.Address);
            }
            return HasRoutableAddress(addresses);
        }

        public static bool HasRoutableAddress(IEnumerable<IPAddress> addresses)
        {
            foreach (IPAddress address in addresses)
            {
                if (IPAddress.IsLoopback(address)) continue;
                if (address.IsIPv6LinkLocal) continue;
                if (IsIPv4LinkLocal(address)) continue;
                return true;
            }
            return false;
        }

        private static bool IsIPv4LinkLocal(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork) return false;

            byte[] octets = address.GetAddressBytes();
            return octets[0] == 169 && octets[1] == 254;
        }

        public string DescribeAdapters()
        {
            StringBuilder description = new StringBuilder();
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                description.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "  {0,-24} {1,-14} {2}", adapter.Name, adapter.OperationalStatus,
                    adapter.Description));
            }
            return description.ToString();
        }

        private void OnNetworkChanged(object sender, EventArgs e)
        {
            EventHandler handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
        }
    }
}
