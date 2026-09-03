using System;

namespace ProxyPacToggler.Core
{
    internal interface IVpnMonitor
    {
        // Raised on an arbitrary thread; the subscriber marshals if it needs to.
        event EventHandler Changed;

        VpnStatus Read(string adapterMatch);

        string DescribeAdapters();
    }
}
