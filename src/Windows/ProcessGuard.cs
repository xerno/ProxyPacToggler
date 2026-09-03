using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    // Named objects die with their process, force kill included; a task would need admin.
    internal static class ProcessGuard
    {
        public const string TrayMutex = @"Local\ProxyPacToggler.SingleInstance";
        public const string WatcherMutex = @"Local\ProxyPacToggler.Watcher";
        public const string StopEvent = @"Local\ProxyPacToggler.Stop";

        public const string WatchArgument = "--watch";

        public static bool IsTrayRunning()
        {
            return Exists(TrayMutex);
        }

        public static bool IsWatcherRunning()
        {
            return Exists(WatcherMutex);
        }

        private static bool Exists(string name)
        {
            Mutex existing;
            if (!Mutex.TryOpenExisting(name, out existing)) return false;

            existing.Dispose();
            return true;
        }

        // Created, not opened, so a watcher still starting up sees the deliberate quit.
        public static void RequestStop()
        {
            using (EventWaitHandle stop = new EventWaitHandle(
                false, EventResetMode.ManualReset, StopEvent))
            {
                stop.Set();
            }
        }

        public static Result Start(string arguments, ILog log)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo(Application.ExecutablePath, arguments);
                start.UseShellExecute = false;
                start.WindowStyle = ProcessWindowStyle.Hidden;
                // The handle would otherwise pile up, once per restart attempt.
                using (Process.Start(start)) { }
                return Result.Ok();
            }
            catch (Exception ex)
            {
                string detail = "could not start \"" + arguments + "\": " + ex.Message;
                log.Write(detail);
                return Result.Fail(detail);
            }
        }
    }
}
