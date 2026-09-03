using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    // Per-user Run key: starting at logon needs no administrator rights.
    internal sealed class AutoStartEntry
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private readonly string valueName;
        private readonly string executablePath;
        private readonly ILog log;

        public AutoStartEntry(string valueName, string executablePath, ILog log)
        {
            if (string.IsNullOrEmpty(valueName)) throw new ArgumentNullException("valueName");
            if (log == null) throw new ArgumentNullException("log");

            this.valueName = valueName;
            this.executablePath = executablePath;
            this.log = log;
        }

        public bool IsEnabled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    {
                        return key != null && key.GetValue(valueName) != null;
                    }
                }
                catch (SecurityException) { return false; }
            }
        }

        public Result Set(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return Result.Fail("registry key HKCU\\" + RunKey + " is missing");

                    if (enabled) key.SetValue(valueName, "\"" + executablePath + "\"",
                                              RegistryValueKind.String);
                    else key.DeleteValue(valueName, false);
                }
            }
            catch (UnauthorizedAccessException ex) { return Failure(ex); }
            catch (SecurityException ex) { return Failure(ex); }
            catch (IOException ex) { return Failure(ex); }

            log.Write("autostart " + (enabled ? "enabled" : "disabled"));
            return Result.Ok();
        }

        private Result Failure(Exception ex)
        {
            string detail = "the autostart entry could not be changed: " + ex.Message;
            log.Write(detail);
            return Result.Fail(detail);
        }
    }
}
