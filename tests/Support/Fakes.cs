using System;
using System.Collections.Generic;
using ProxyPacToggler.Cli;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Tests
{
    internal sealed class FakeLog : ILog
    {
        public readonly List<string> Lines = new List<string>();

        public string Location { get { return @"C:\nowhere\log.txt"; } }

        public void Write(string message)
        {
            Lines.Add(message);
        }

        public bool Mentions(string fragment)
        {
            foreach (string line in Lines)
            {
                if (line.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }

    internal sealed class FakeProxySettings : IProxySettings
    {
        private string current = "";

        public int ApplyCount;
        public int ClearCount;
        public string FailWith;

        public string CurrentPacUrl { get { return current; } }

        public bool IsPacEnabled { get { return current.Length > 0; } }

        public Result Apply(string pacUrl)
        {
            ApplyCount++;
            if (FailWith != null) return Result.Fail(FailWith);

            Result validated = PacUrlValidator.Validate(pacUrl);
            if (!validated.Succeeded) return validated;

            current = pacUrl.Trim();
            return Result.Ok();
        }

        // Simulates the command line or another tool writing the value directly.
        public void ChangeExternally(string pacUrl)
        {
            current = pacUrl == null ? "" : pacUrl;
        }

        public Result Clear()
        {
            ClearCount++;
            if (FailWith != null) return Result.Fail(FailWith);

            current = "";
            return Result.Ok();
        }
    }

    internal sealed class FakeVpnMonitor : IVpnMonitor
    {
        public event EventHandler Changed;

        public bool IsUp;
        public bool MatchesAnAdapter = true;
        public string LastPatternAsked;
        public int ReadCount;

        public VpnStatus Read(string adapterMatch)
        {
            LastPatternAsked = adapterMatch;
            ReadCount++;
            return new VpnStatus(IsUp, MatchesAnAdapter,
                                 MatchesAnAdapter ? (IsUp ? "Fake=Up" : "Fake=Down") : "no adapter");
        }

        public string DescribeAdapters()
        {
            return "  Fake  Up  Fake adapter\n";
        }

        public void RaiseChanged()
        {
            EventHandler handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }

    internal sealed class FakeSettingsStore : ISettingsStore
    {
        private Settings stored;

        public int SaveCount;
        public string FailWith;

        public FakeSettingsStore() : this(new Settings())
        {
        }

        public FakeSettingsStore(Settings initial)
        {
            stored = initial;
        }

        public string Location { get { return @"C:\nowhere\config.ini"; } }

        public Settings Load()
        {
            return stored.Copy();
        }

        public Result Save(Settings settings)
        {
            SaveCount++;
            if (FailWith != null) return Result.Fail(FailWith);

            stored = settings.Copy();
            return Result.Ok();
        }
    }

    internal sealed class RecordingConsole : IConsole
    {
        public readonly List<string> Output = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public void Write(string line)
        {
            Output.Add(line);
        }

        public void WriteRaw(string text)
        {
            Output.Add(text);
        }

        public void WriteError(string line)
        {
            Errors.Add(line);
        }

        public string AllOutput { get { return string.Join("\n", Output.ToArray()); } }

        public string AllErrors { get { return string.Join("\n", Errors.ToArray()); } }
    }
}
