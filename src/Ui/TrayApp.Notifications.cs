using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Ui
{
    internal sealed partial class TrayApp
    {
        private bool reportedMissingAdapter;

        // Said once per session, and worded to inform rather than alarm.
        private void ReportMissingAdapter()
        {
            // Normal on the default list; only a pattern the user chose is questionable.
            if (controller.Settings.AdapterMatch == Settings.DefaultAdapterMatch) return;
            if (reportedMissingAdapter) return;
            reportedMissingAdapter = true;

            Report("No network adapter matched "
                       + NetworkVpnMonitor.Describe(controller.Settings.AdapterMatch),
                   "Normal if your VPN removes its adapter while disconnected. "
                       + "Otherwise use \"Set VPN adapter...\" or --list.");
        }

        // The pair keeps each other alive: whichever survives restarts the other.
        private void EnsureWatcher()
        {
            if (ProcessGuard.IsWatcherRunning()) return;

            log.Write("watcher is missing, starting it");
            ProcessGuard.Start(ProcessGuard.WatchArgument, log);
        }

        // Icons are cached because Refresh runs on every timer tick.
        private Icon IconFor(bool pacEnabled, bool hasProblem)
        {
            string key = (pacEnabled ? "on" : "off") + (hasProblem ? "!" : "");
            if (!icons.ContainsKey(key)) icons[key] = IconFactory.Create(pacEnabled, hasProblem);
            return icons[key];
        }

        // An exception reaching the message loop would leave the icon alive but inert.
        private void Guarded(string action, Action body)
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                log.Write(action + " failed: " + ex);
                Report("Something went wrong while " + action, ex.Message);
            }
        }

        private void Report(string what, string detail)
        {
            log.Write(what + ": " + detail);

            if (!problems.Record(what, detail, DateTime.Now)) return;

            // Windows 10 and later render this as a toast, reaching the notification centre.
            tray.BalloonTipTitle = what;
            tray.BalloonTipText = detail;
            tray.BalloonTipIcon = ToolTipIcon.Warning;
            tray.ShowBalloonTip(NotificationMs);
        }
    }
}
