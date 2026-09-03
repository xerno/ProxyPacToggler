using ProxyPacToggler.Core;

namespace ProxyPacToggler.Tests
{
    internal static class PacUrlValidatorTests
    {
        public static void Run()
        {
            Assert.Case("PacUrlValidator accepts real PAC URLs");
            Accepts("http://pac.example.com/proxy.pac");
            Accepts("https://pac.example.com/proxy.pac");
            Accepts("http://proxy.example.org/tenant/config.pac");
            Accepts("http://pac.example.com/proxy.pac?user=abc&mode=1");
            Accepts("file://C:/proxy/local.pac");
            Accepts("  http://pac.example.com/proxy.pac  ");

            Assert.Case("PacUrlValidator rejects dangerous or malformed input");
            Rejects(null, "null");
            Rejects("", "empty");
            Rejects("   ", "whitespace only");
            Rejects("proxy.pac", "a bare file name");
            Rejects("/etc/proxy.pac", "a path");
            Rejects("javascript:alert(1)", "the javascript scheme");
            Rejects("data:text/plain,FindProxyForURL", "the data scheme");
            Rejects("ftp://pac.example.com/proxy.pac", "the ftp scheme");
            Rejects("http:///proxy.pac", "a missing host");
            Rejects("http://pac.example.com/\r\nHost: evil", "control characters");
            Rejects("http://pac.example.com/" + new string('a', PacUrlValidator.MaxLength),
                    "a URL over the length limit");

            Assert.Case("PacUrlValidator normalises");
            string normalized;
            string error;
            Assert.True(PacUrlValidator.TryNormalize("  http://pac.example.com/p.pac  ",
                                                     out normalized, out error),
                        "padded input is accepted");
            Assert.Equal("http://pac.example.com/p.pac", normalized, "surrounding space is trimmed");
        }

        private static void Accepts(string url)
        {
            Result result = PacUrlValidator.Validate(url);
            Assert.True(result.Succeeded, "accepts \"" + url + "\" (rejected with: " + result.Error + ")");
        }

        private static void Rejects(string url, string what)
        {
            Result result = PacUrlValidator.Validate(url);
            Assert.False(result.Succeeded, "rejects " + what);
            Assert.True(result.Succeeded || result.Error.Length > 0, "explains why it rejects " + what);
        }
    }
}
