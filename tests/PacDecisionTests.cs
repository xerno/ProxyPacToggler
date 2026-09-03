using ProxyPacToggler.Core;

namespace ProxyPacToggler.Tests
{
    internal static class PacDecisionTests
    {
        public static void Run()
        {
            Assert.Case("PacDecision.WantsPac");
            Assert.True(PacDecision.WantsPac(true, false), "VPN up, normal: PAC on");
            Assert.False(PacDecision.WantsPac(false, false), "VPN down, normal: PAC off");
            Assert.False(PacDecision.WantsPac(true, true), "VPN up, inverted: PAC off");
            Assert.True(PacDecision.WantsPac(false, true), "VPN down, inverted: PAC on");
        }
    }
}
