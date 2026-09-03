namespace ProxyPacToggler.Core
{
    internal interface ISettingsStore
    {
        string Location { get; }

        Settings Load();

        Result Save(Settings settings);
    }
}
