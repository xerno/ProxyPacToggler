namespace ProxyPacToggler.Core
{
    internal interface IProxySettings
    {
        string CurrentPacUrl { get; }

        bool IsPacEnabled { get; }

        Result Apply(string pacUrl);

        Result Clear();
    }
}
