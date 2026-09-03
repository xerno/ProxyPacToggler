using System.Collections.Generic;
using System.Net;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Tests
{
    internal static class TunnelReadinessTests
    {
        public static void Run()
        {
            Assert.Case("an adapter with a routable address counts as connected");
            Assert.True(Has("10.20.30.40"), "a private tunnel address counts");
            Assert.True(Has("192.168.1.50"), "a LAN address counts");
            Assert.True(Has("2001:db8::1"), "a global IPv6 address counts");
            Assert.True(Has("fd00::1"), "a unique-local IPv6 address counts");
            Assert.True(Has("169.254.1.1", "10.20.30.40"),
                        "one routable address among unusable ones is enough");

            Assert.Case("an adapter without one does not");
            Assert.False(Has(), "no addresses at all");
            Assert.False(Has("169.254.10.20"), "an IPv4 link-local address does not count");
            Assert.False(Has("fe80::1"), "an IPv6 link-local address does not count");
            Assert.False(Has("127.0.0.1"), "loopback does not count");
            Assert.False(Has("::1"), "IPv6 loopback does not count");
            Assert.False(Has("fe80::1", "169.254.5.5", "127.0.0.1"),
                         "a mix of unusable addresses still does not count");
        }

        private static bool Has(params string[] addresses)
        {
            List<IPAddress> parsed = new List<IPAddress>();
            foreach (string address in addresses) parsed.Add(IPAddress.Parse(address));
            return NetworkVpnMonitor.HasRoutableAddress(parsed);
        }
    }
}
