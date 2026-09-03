using System;

namespace ProxyPacToggler.Core
{
    // A PAC script routes every request, so a bad URL here is interception, not a typo.
    internal static class PacUrlValidator
    {
        public const int MaxLength = 2048;

        private static readonly string[] AllowedSchemes = { "http", "https", "file" };

        public static Result Validate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                return Result.Fail("the PAC URL is empty");

            string candidate = value.Trim();

            if (candidate.Length > MaxLength)
                return Result.Fail("the PAC URL is longer than " + MaxLength + " characters");

            foreach (char c in candidate)
            {
                if (char.IsControl(c))
                    return Result.Fail("the PAC URL contains a control character");
            }

            Uri uri;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri))
                return Result.Fail("the PAC URL is not an absolute URL");

            if (Array.IndexOf(AllowedSchemes, uri.Scheme) < 0)
                return Result.Fail("the scheme \"" + uri.Scheme + "\" is not allowed; use "
                                   + string.Join(", ", AllowedSchemes));

            if (uri.Scheme != Uri.UriSchemeFile && string.IsNullOrEmpty(uri.Host))
                return Result.Fail("the PAC URL has no host");

            return Result.Ok();
        }

        public static bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value == null ? "" : value.Trim();
            Result result = Validate(normalized);
            error = result.Error;
            return result.Succeeded;
        }
    }
}
