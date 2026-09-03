namespace ProxyPacToggler.Core
{
    internal sealed class VpnStatus
    {
        public VpnStatus(bool isUp, bool matchedAny, string detail)
        {
            IsUp = isUp;
            MatchedAny = matchedAny;
            Detail = detail;
        }

        public bool IsUp { get; private set; }

        // False when nothing matched, usually a renamed or removed adapter.
        public bool MatchedAny { get; private set; }

        // What the adapters looked like, so the log explains every decision.
        public string Detail { get; private set; }
    }
}
