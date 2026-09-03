namespace ProxyPacToggler.Core
{
    internal sealed class SyncOutcome
    {
        private SyncOutcome(Result result, string externalChange, bool adapterMissing)
        {
            Result = result;
            ExternalChange = externalChange;
            AdapterMissing = adapterMissing;
        }

        public Result Result { get; private set; }

        // Non-null when something outside this app changed the proxy setting.
        public string ExternalChange { get; private set; }

        public bool AdapterMissing { get; private set; }

        public static SyncOutcome Of(Result result, string externalChange, bool adapterMissing)
        {
            return new SyncOutcome(result, externalChange, adapterMissing);
        }
    }
}
