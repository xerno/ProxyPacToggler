using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    internal sealed class RegistryProxySettings : IProxySettings
    {
        public const string WindowsProxyKey =
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        private const string ValueName = "AutoConfigURL";

        private readonly string keyPath;
        private readonly ILog log;

        public RegistryProxySettings(ILog log) : this(WindowsProxyKey, log)
        {
        }

        // Tests pass a scratch key so they never touch the machine's real proxy.
        public RegistryProxySettings(string keyPath, ILog log)
        {
            if (string.IsNullOrEmpty(keyPath)) throw new ArgumentNullException("keyPath");
            if (log == null) throw new ArgumentNullException("log");

            this.keyPath = keyPath;
            this.log = log;
        }

        public string CurrentPacUrl
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, false))
                    {
                        if (key == null) return "";
                        object value = key.GetValue(ValueName);
                        return value == null ? "" : value.ToString();
                    }
                }
                catch (SecurityException ex)
                {
                    log.Write("cannot read the current PAC setting: " + ex.Message);
                    return "";
                }
            }
        }

        public bool IsPacEnabled { get { return CurrentPacUrl.Trim().Length > 0; } }

        public Result Apply(string pacUrl)
        {
            string normalized;
            string error;
            if (!PacUrlValidator.TryNormalize(pacUrl, out normalized, out error))
            {
                log.Write("PAC not enabled: " + error);
                return Result.Fail(error);
            }

            if (CurrentPacUrl == normalized) return AlreadyThere("on");

            Result written = Write(normalized);
            if (written.Succeeded) log.Write("PAC ON  -> " + normalized);
            return written;
        }

        public Result Clear()
        {
            string current = CurrentPacUrl;
            if (current.Trim().Length == 0) return AlreadyThere("off");

            Result written = Write(null);
            if (written.Succeeded) log.Write("PAC OFF (was " + current + ")");
            return written;
        }

        // Logged even though nothing was written, so the log states every outcome.
        private Result AlreadyThere(string state)
        {
            log.Write("PAC " + state + " already, nothing to write");
            return Result.Ok();
        }

        // A write can be accepted and then ignored, typically under group policy.
        private Result Write(string pacUrl)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    if (key == null) return Result.Fail("registry key HKCU\\" + keyPath + " is missing");

                    if (pacUrl == null) key.DeleteValue(ValueName, false);
                    else key.SetValue(ValueName, pacUrl, RegistryValueKind.String);
                }
            }
            catch (UnauthorizedAccessException ex) { return Failure("access denied", ex); }
            catch (SecurityException ex) { return Failure("access denied", ex); }
            catch (IOException ex) { return Failure("registry write failed", ex); }

            Native.NotifyProxyChanged();
            return VerifyStuck(pacUrl == null ? "" : pacUrl);
        }

        private Result VerifyStuck(string expected)
        {
            string actual = CurrentPacUrl;
            if (actual == expected) return Result.Ok();

            string detail = "the setting did not stick (wanted \"" + expected
                            + "\", found \"" + actual + "\")" + ProxyPolicy.ManagedHint();
            log.Write("PAC write verification failed: " + detail);
            return Result.Fail(detail);
        }

        private Result Failure(string what, Exception ex)
        {
            string detail = what + ": " + ex.Message;
            log.Write("PAC write failed, " + detail);
            return Result.Fail(detail);
        }
    }
}
