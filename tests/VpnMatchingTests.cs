using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Tests
{
    internal static class VpnMatchingTests
    {
        public static void Run()
        {
            SinglePattern();
            PatternList();
            TheDefault();
            Descriptions();
        }

        private static void SinglePatternCases(string pattern)
        {
            Assert.True(NetworkVpnMonitor.Matches("Ethernet 3", "Contoso VPN Client Adapter", pattern),
                        "matches inside a description");
            Assert.True(NetworkVpnMonitor.Matches("Contoso VPN", "Unknown device", pattern),
                        "matches inside an adapter name");
        }

        private static void SinglePattern()
        {
            Assert.Case("Matches with one pattern");
            SinglePatternCases("Contoso VPN");
            SinglePatternCases("contoso vpn");

            Assert.False(NetworkVpnMonitor.Matches("Ethernet", "Realtek Gaming GbE", "Contoso VPN"),
                         "does not match an unrelated adapter");
            Assert.False(NetworkVpnMonitor.Matches("Ethernet 3", "Contoso VPN Client", ""),
                         "an empty pattern matches nothing rather than everything");
            Assert.False(NetworkVpnMonitor.Matches(null, null, "Contoso"),
                         "a null name and description do not throw");
        }

        private static void PatternList()
        {
            const string list = "WireGuard, OpenVPN, Contoso VPN";

            Assert.Case("Matches with a comma-separated list");
            Assert.True(NetworkVpnMonitor.Matches("wg0", "WireGuard Tunnel", list),
                        "matches the first entry");
            Assert.True(NetworkVpnMonitor.Matches("Ethernet 7", "Contoso VPN Client Adapter", list),
                        "matches a multi-word entry");
            Assert.False(NetworkVpnMonitor.Matches("Ethernet", "Realtek Gaming GbE", list),
                         "matches nothing when no entry applies");

            Assert.Case("the list tolerates sloppy formatting");
            Assert.True(NetworkVpnMonitor.Matches("wg0", "WireGuard Tunnel", " , WireGuard ,, "),
                        "extra commas and spaces are ignored");
            Assert.False(NetworkVpnMonitor.Matches("wg0", "WireGuard Tunnel", " , ,, "),
                         "a list of nothing still matches nothing");
        }

        private static void TheDefault()
        {
            string standard = Settings.DefaultAdapterMatch;

            Assert.Case("the default covers the common VPN clients");
            Assert.True(Matches("AnyConnect Secure Mobility Client Virtual Miniport Adapter",
                                standard), "AnyConnect");
            Assert.True(Matches("Fortinet SSL VPN Virtual Ethernet Adapter", standard), "FortiClient");
            Assert.True(Matches("PANGP Virtual Ethernet Adapter Secure GlobalProtect", standard),
                        "GlobalProtect");
            Assert.True(Matches("Tailscale Tunnel", standard), "Tailscale");
            Assert.True(Matches("TAP-Windows Adapter V9", standard), "OpenVPN via TAP-Windows");
            Assert.True(Matches("WireGuard Tunnel", standard), "WireGuard");
            Assert.True(Matches("Some Unknown VPN Adapter", standard), "anything calling itself a VPN");

            Assert.Case("the default ignores ordinary adapters");
            Assert.False(Matches("Realtek PCIe GbE Family Controller", standard), "a wired NIC");
            Assert.False(Matches("Intel Wi-Fi 6E AX211 160MHz", standard), "a wireless NIC");
            Assert.False(Matches("Hyper-V Virtual Ethernet Adapter", standard), "a hypervisor adapter");
            Assert.False(Matches("Bluetooth Device (Personal Area Network)", standard), "Bluetooth");
        }

        private static bool Matches(string description, string patterns)
        {
            return NetworkVpnMonitor.Matches("Ethernet 3", description, patterns);
        }

        private static void Descriptions()
        {
            Assert.Case("Describe quotes one pattern and counts many");
            Assert.Equal("\"Contoso VPN\"", NetworkVpnMonitor.Describe("Contoso VPN"),
                         "a single pattern is quoted");
            Assert.Equal("\"Contoso VPN\"", NetworkVpnMonitor.Describe("  Contoso VPN , "),
                         "padding and a trailing comma do not make it plural");
            Assert.Equal("any of 3 configured patterns",
                         NetworkVpnMonitor.Describe("WireGuard, OpenVPN, Contoso VPN"),
                         "several patterns are counted rather than listed");
            Assert.Equal("any of 15 configured patterns",
                         NetworkVpnMonitor.Describe(Settings.DefaultAdapterMatch),
                         "the default list is counted, not spelled out");
            Assert.Equal("any of 0 configured patterns", NetworkVpnMonitor.Describe(""),
                         "an empty setting is not mistaken for a pattern");
        }
    }
}
