namespace ProxyPacToggler.Core
{
    // Registry and network operations fail for reasons the user needs to hear.
    internal sealed class Result
    {
        private static readonly Result Success = new Result(true, "");

        private Result(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public bool Succeeded { get; private set; }
        public string Error { get; private set; }

        public static Result Ok()
        {
            return Success;
        }

        public static Result Fail(string error)
        {
            return new Result(false, error);
        }
    }
}
