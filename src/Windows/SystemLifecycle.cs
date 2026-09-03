using System;
using Microsoft.Win32;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    // A logged shutdown is what later distinguishes a deliberate close from a crash.
    internal sealed class SystemLifecycle : IDisposable
    {
        private readonly ILog log;

        public event EventHandler Resumed;

        public SystemLifecycle(ILog log)
        {
            if (log == null) throw new ArgumentNullException("log");
            this.log = log;

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionEnding += OnSessionEnding;
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend) log.Write("system suspending");
            if (e.Mode != PowerModes.Resume) return;

            log.Write("system resumed");
            EventHandler handler = Resumed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            log.Write("session ending (" + e.Reason + "), shutting down cleanly");
        }

        public void Dispose()
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionEnding -= OnSessionEnding;
        }
    }
}
