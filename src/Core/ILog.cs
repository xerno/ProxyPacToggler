namespace ProxyPacToggler.Core
{
    internal interface ILog
    {
        string Location { get; }

        void Write(string message);
    }
}
