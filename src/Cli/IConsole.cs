namespace ProxyPacToggler.Cli
{
    // An interface so the command line can be tested without a console attached.
    internal interface IConsole
    {
        void Write(string line);

        void WriteRaw(string text);

        void WriteError(string line);
    }
}
