using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace ProxyPacToggler.Ui
{
    internal sealed partial class TrayApp : IDisposable
    {
        // A VPN client fires several network events while connecting; wait for quiet.
        private const int DebounceMs = 1500;

        // Backstop for an event that never arrives.
        private const int SafetyIntervalMs = 60000;

        private const int NotificationMs = 8000;
        private const int OffScreen = -32000;

        private readonly ProxyController controller;
        private readonly AutoStartEntry autoStart;
        private readonly IVpnMonitor monitor;
        private readonly SystemLifecycle lifecycle;
        private readonly ILog log;

        private readonly Form anchor;
        private readonly NotifyIcon tray;
        private readonly TrayMenu menu;
        private readonly WinFormsTimer debounce;
        private readonly WinFormsTimer safety;
        private readonly ProblemLog problems = new ProblemLog();
        private readonly Dictionary<string, Icon> icons = new Dictionary<string, Icon>();

        public TrayApp(ProxyController controller, IVpnMonitor monitor,
                       SystemLifecycle lifecycle, AutoStartEntry autoStart, ILog log)
        {
            this.controller = controller;
            this.monitor = monitor;
            this.lifecycle = lifecycle;
            this.autoStart = autoStart;
            this.log = log;

            anchor = CreateAnchorWindow();

            menu = new TrayMenu();
            WireMenu();

            tray = new NotifyIcon();
            tray.ContextMenuStrip = menu.Strip;
            tray.MouseClick += OnTrayClicked;

            debounce = NewTimer(DebounceMs, OnDebounceElapsed);
            safety = NewTimer(SafetyIntervalMs, OnSafetyElapsed);
        }

        public void Start()
        {
            monitor.Changed += OnNetworkChanged;
            lifecycle.Resumed += OnSystemResumed;
            tray.Visible = true;
            safety.Start();

            using (Process self = Process.GetCurrentProcess()) log.Write("started, pid " + self.Id);
            EnsureWatcher();
            Synchronise(true, "start");
        }

        // Exists only to own the UI thread's message loop that events marshal onto.
        private static Form CreateAnchorWindow()
        {
            Form form = new Form();
            form.ShowInTaskbar = false;
            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form.Opacity = 0;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(OffScreen, OffScreen);
            GC.KeepAlive(form.Handle);
            return form;
        }

        private static WinFormsTimer NewTimer(int intervalMs, EventHandler onTick)
        {
            WinFormsTimer timer = new WinFormsTimer();
            timer.Interval = intervalMs;
            timer.Tick += onTick;
            return timer;
        }

        private void WireMenu()
        {
            menu.Strip.Opening += OnMenuOpening;
            menu.Problem.Click += OnProblemClicked;
            menu.Toggle.Click += OnToggleClicked;
            menu.FollowVpn.Click += OnFollowVpnClicked;
            menu.Invert.Click += OnInvertClicked;
            menu.SetUrl.Click += OnSetUrlClicked;
            menu.SetAdapter.Click += OnSetAdapterClicked;
            menu.AutoStart.Click += OnAutoStartClicked;
            menu.OpenLog.Click += OnOpenLogClicked;
            menu.Exit.Click += OnExitClicked;
        }

        private void OnNetworkChanged(object sender, EventArgs e)
        {
            MarshalToUi("network");
        }

        private void OnSystemResumed(object sender, EventArgs e)
        {
            MarshalToUi("resume");
        }

        // Off the UI thread: touching UI state is illegal and throwing kills the process.
        private void MarshalToUi(string source)
        {
            try
            {
                if (anchor.IsHandleCreated && !anchor.IsDisposed)
                    anchor.BeginInvoke((MethodInvoker)delegate { RestartDebounce(); });
            }
            catch (Exception ex)
            {
                log.Write(source + " event marshalling failed: " + ex);
            }
        }

        private void RestartDebounce()
        {
            log.Write("network event");
            debounce.Stop();
            debounce.Start();
        }

        private void OnDebounceElapsed(object sender, EventArgs e)
        {
            debounce.Stop();
            Synchronise(false, "event");
        }

        private void OnSafetyElapsed(object sender, EventArgs e)
        {
            EnsureWatcher();
            Synchronise(false, "safety");
        }

        private void Synchronise(bool force, string trigger)
        {
            Guarded("following the VPN", delegate
            {
                SyncOutcome outcome = controller.Synchronise(force, trigger);

                if (outcome.ExternalChange != null)
                    Report("The proxy setting was changed by something else",
                           outcome.ExternalChange + ". Set back.");

                if (outcome.AdapterMissing) ReportMissingAdapter();

                if (!outcome.Result.Succeeded)
                    Report("Could not switch the PAC script", outcome.Result.Error);

                Refresh();
            });
        }

        private void Refresh()
        {
            bool pacEnabled = controller.IsPacEnabled;
            Icon icon = IconFor(pacEnabled, problems.HasProblem);
            string tooltip = TrayTooltip.Compose(pacEnabled, controller.Settings.FollowVpn,
                                                 controller.LastVpn.IsUp, problems.HasProblem);

            // Each assignment notifies the shell, and this runs on every safety tick.
            if (tray.Icon != icon) tray.Icon = icon;
            if (tray.Text != tooltip) tray.Text = tooltip;
        }

        // On demand: reading the autostart entry on every tick would be for nothing.
        private void OnMenuOpening(object sender, CancelEventArgs e)
        {
            Guarded("opening the menu", delegate
            {
                menu.Reflect(controller.IsPacEnabled, controller.Settings,
                             autoStart.IsEnabled, problems.ShortLine());
            });
        }

        public void Dispose()
        {
            monitor.Changed -= OnNetworkChanged;
            lifecycle.Resumed -= OnSystemResumed;
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            debounce.Dispose();
            safety.Dispose();
            anchor.Dispose();
            foreach (Icon icon in icons.Values) icon.Dispose();
        }
    }
}
