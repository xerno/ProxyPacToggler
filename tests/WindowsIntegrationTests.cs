using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Tests
{
    // Real registry and disk, but only a scratch key and a temp directory.
    internal static class WindowsIntegrationTests
    {
        private const string ScratchKey = @"Software\ProxyPacToggler.Tests";
        private const string Url = "http://pac.example.com/proxy.pac";

        public static void Run(string scratchDirectory)
        {
            RegistryProxy();
            SettingsFile(scratchDirectory);
            LogFile(scratchDirectory);
        }

        private static void RegistryProxy()
        {
            FakeLog log = new FakeLog();
            RegistryProxySettings proxy = new RegistryProxySettings(ScratchKey, log);
            Registry.CurrentUser.CreateSubKey(ScratchKey);

            try
            {
                Assert.Case("RegistryProxySettings writes and clears");
                proxy.Clear();
                Assert.False(proxy.IsPacEnabled, "starts off");

                Assert.True(proxy.Apply(Url).Succeeded, "applying succeeds");
                Assert.True(proxy.IsPacEnabled, "the PAC script reads back as on");
                Assert.Equal(Url, proxy.CurrentPacUrl, "with the URL that was written");

                Assert.True(proxy.Apply(Url).Succeeded, "applying twice is harmless");
                Assert.True(proxy.Clear().Succeeded, "clearing succeeds");
                Assert.Equal("", proxy.CurrentPacUrl, "the value is gone");
                Assert.True(proxy.Clear().Succeeded, "clearing an already-clear setting is fine");

                Assert.Case("a no-op still says so, so the log accounts for every outcome");
                log.Lines.Clear();
                proxy.Clear();
                Assert.True(log.Mentions("PAC off already"), "clearing what is already off is logged");
                proxy.Apply(Url);
                log.Lines.Clear();
                proxy.Apply(Url);
                Assert.True(log.Mentions("PAC on already"), "applying what is already set is logged");
                proxy.Clear();

                Assert.Case("RegistryProxySettings refuses unsafe input");
                Assert.False(proxy.Apply("javascript:alert(1)").Succeeded, "a javascript URL is refused");
                Assert.False(proxy.IsPacEnabled, "and nothing was written");
                Assert.False(proxy.Apply("proxy.pac").Succeeded, "a relative URL is refused");
                Assert.False(proxy.IsPacEnabled, "still nothing written");

                Assert.Case("RegistryProxySettings normalises before writing");
                Assert.True(proxy.Apply("  " + Url + "  ").Succeeded, "padded input is accepted");
                Assert.Equal(Url, proxy.CurrentPacUrl, "the stored value is trimmed");
                proxy.Clear();

                Assert.Case("the real Windows proxy key is never the target here");
                Assert.False(ScratchKey == RegistryProxySettings.WindowsProxyKey,
                             "the scratch key differs from the real one");
            }
            finally
            {
                try { Registry.CurrentUser.DeleteSubKeyTree(ScratchKey, false); }
                catch (ArgumentException) { }
            }
        }

        private static void SettingsFile(string directory)
        {
            FakeLog log = new FakeLog();
            IniSettingsStore store = new IniSettingsStore(directory, log);

            Assert.Case("IniSettingsStore round-trips");
            Settings written = new Settings();
            written.PacUrl = Url;
            written.FollowVpn = false;
            written.Inverted = true;
            written.AdapterMatch = "WireGuard";
            Assert.True(store.Save(written).Succeeded, "saving succeeds");

            Settings read = store.Load();
            Assert.Equal(Url, read.PacUrl, "the URL survives");
            Assert.False(read.FollowVpn, "FollowVpn survives");
            Assert.True(read.Inverted, "Inverted survives");
            Assert.Equal("WireGuard", read.AdapterMatch, "AdapterMatch survives");

            Assert.Case("IniSettingsStore keeps a URL containing '='");
            string tricky = "http://pac.example.com/proxy.pac?user=abc&mode=1";
            Write(store, "PacUrl=" + tricky);
            Assert.Equal(tricky, store.Load().PacUrl, "only the first '=' separates key from value");

            Assert.Case("IniSettingsStore ignores junk");
            Write(store, "\r\n# a comment\r\n   \r\nNoEqualsSign\r\n=leading equals\r\n"
                         + "Unknown=whatever\r\nAdapterMatch=Tailscale");
            Settings afterJunk = store.Load();
            Assert.Equal("Tailscale", afterJunk.AdapterMatch, "a valid line after junk still applies");
            Assert.Equal("", afterJunk.PacUrl, "junk did not invent a URL");

            Assert.Case("IniSettingsStore refuses an unsafe stored URL");
            Write(store, "PacUrl=javascript:alert(1)");
            Assert.Equal("", store.Load().PacUrl, "a javascript URL in the file is ignored");
            Write(store, "PacUrl=not-a-url");
            Assert.Equal("", store.Load().PacUrl, "a malformed URL in the file is ignored");

            Assert.Case("IniSettingsStore ignores an oversized file");
            StringBuilder oversized = new StringBuilder("AdapterMatch=ShouldBeIgnored\r\n");
            while (oversized.Length < 70 * 1024) oversized.AppendLine("# padding padding padding");
            Write(store, oversized.ToString());
            Assert.Equal(Settings.DefaultAdapterMatch, store.Load().AdapterMatch,
                         "nothing from an oversized file is applied");

            Assert.Case("IniSettingsStore skips an absurdly long line");
            Write(store, "AdapterMatch=" + new string('x', 5000) + "\r\nInvert=1");
            Settings afterLongLine = store.Load();
            Assert.Equal(Settings.DefaultAdapterMatch, afterLongLine.AdapterMatch, "the long line is skipped");
            Assert.True(afterLongLine.Inverted, "later valid lines still apply");

            Assert.Case("IniSettingsStore leaves no temporary file behind");
            Settings clean = new Settings();
            clean.PacUrl = Url;
            store.Save(clean);
            Assert.False(File.Exists(store.Location + ".tmp"), "the temporary file is gone");
            Assert.Equal(Url, store.Load().PacUrl, "and the real file is intact");

            Assert.Case("IniSettingsStore keeps the old file when the swap cannot happen");
            using (new FileStream(store.Location, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Settings attempted = new Settings();
                attempted.PacUrl = "http://pac.example.com/other.pac";
                Assert.False(store.Save(attempted).Succeeded, "saving fails while the file is locked");
            }
            Assert.Equal(Url, store.Load().PacUrl, "the previous settings survived");
            Assert.False(File.Exists(store.Location + ".tmp"), "and no temporary file was left");

            Assert.Case("IniSettingsStore falls back to defaults with no file");
            File.Delete(store.Location);
            Assert.Equal(Settings.DefaultAdapterMatch, store.Load().AdapterMatch, "defaults are used");
        }

        private static void Write(IniSettingsStore store, string content)
        {
            File.WriteAllText(store.Location, content, Encoding.UTF8);
        }

        private static void LogFile(string directory)
        {
            string logDirectory = Path.Combine(directory, "log-test");
            FileLog log = new FileLog(logDirectory);

            Assert.Case("FileLog writes and timestamps");
            log.Write("hello");
            Assert.True(File.Exists(log.Location), "the file is created on first write");
            string content = File.ReadAllText(log.Location);
            Assert.True(content.Contains("hello"), "the message is there");
            Assert.True(content.Contains(DateTime.Now.ToString("yyyy-MM-dd")), "the line is timestamped");

            Assert.Case("FileLog trims itself");
            StringBuilder oversized = new StringBuilder();
            string filler = new string('x', 200);
            for (int i = 0; i < 2000; i++) oversized.AppendLine(filler);
            File.WriteAllText(log.Location, oversized.ToString(), Encoding.UTF8);
            Assert.True(new FileInfo(log.Location).Length > 200 * 1024, "the log is oversized");

            log.Write("this write should trigger a trim");
            Assert.True(new FileInfo(log.Location).Length < 200 * 1024, "the log came back under the limit");

            string[] lines = File.ReadAllLines(log.Location);
            Assert.True(lines.Length <= 301, "only the tail was kept (got " + lines.Length + ")");
            Assert.True(lines[lines.Length - 1].Contains("trigger a trim"), "the newest line survived");

            Assert.Case("FileLog tolerates a reader holding the file open");
            using (new FileStream(log.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                log.Write("written while the file is open elsewhere");
            }
            Assert.True(File.ReadAllText(log.Location).Contains("open elsewhere"),
                        "the line was still appended");
        }
    }
}
