using ProxyPacToggler.Ui;

namespace ProxyPacToggler.Tests
{
    // Every combination is checked, not just the likely ones.
    internal static class TrayTooltipTests
    {
        public static void Run()
        {
            Assert.Case("TrayTooltip never exceeds what NotifyIcon accepts");
            foreach (bool pac in new[] { true, false })
            {
                foreach (bool auto in new[] { true, false })
                {
                    foreach (bool vpn in new[] { true, false })
                    {
                        foreach (bool problem in new[] { true, false })
                        {
                            string text = TrayTooltip.Compose(pac, auto, vpn, problem);
                            Assert.True(text.Length <= TrayTooltip.MaxLength,
                                        "pac=" + pac + " auto=" + auto + " vpn=" + vpn
                                        + " problem=" + problem + " fits in "
                                        + TrayTooltip.MaxLength + " (was " + text.Length + ")");
                        }
                    }
                }
            }

            Assert.Case("TrayTooltip says what matters");
            Assert.True(TrayTooltip.Compose(true, true, true, false).Contains("PAC: ON"),
                        "an enabled PAC script is named");
            Assert.True(TrayTooltip.Compose(false, true, false, false).Contains("PAC: off"),
                        "a disabled one too");
            Assert.True(TrayTooltip.Compose(true, false, true, false).Contains("manual"),
                        "manual mode is visible");
            Assert.True(TrayTooltip.Compose(true, true, true, false).Contains("auto"),
                        "automatic mode is visible");
            Assert.True(TrayTooltip.Compose(true, true, false, false).Contains("disconnected"),
                        "the VPN state is visible");
            Assert.True(TrayTooltip.Compose(true, true, true, true).Contains("problem"),
                        "a pending problem is flagged");
            Assert.False(TrayTooltip.Compose(true, true, true, false).Contains("problem"),
                         "and not flagged when there is none");
        }
    }
}
