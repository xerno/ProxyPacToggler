using System;
using System.Threading;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    // The other half of the pair: no window, no icon, no timers.
    internal sealed class Watcher
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        private readonly ILog log;

        public Watcher(ILog log)
        {
            this.log = log;
        }

        public int Run()
        {
            bool acquired;
            using (new Mutex(true, ProcessGuard.WatcherMutex, out acquired))
            {
                if (!acquired) return 0;

                using (EventWaitHandle stop = new EventWaitHandle(
                    false, EventResetMode.ManualReset, ProcessGuard.StopEvent))
                {
                    log.Write("watcher started");
                    Loop(stop);
                }

                log.Write("watcher stopped");
                return 0;
            }
        }

        private void Loop(EventWaitHandle stop)
        {
            while (!stop.WaitOne(PollInterval))
            {
                if (ProcessGuard.IsTrayRunning()) continue;

                log.Write("tray is gone, restarting it");
                ProcessGuard.Start("", log);

                // Give it time to take the mutex before looking again.
                if (stop.WaitOne(PollInterval)) return;
            }
        }
    }
}
