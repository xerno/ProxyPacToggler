using System;
using System.Windows.Forms;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Ui
{
    internal sealed class TrayMenu : IDisposable
    {
        private readonly ToolStripSeparator afterProblem;

        public TrayMenu()
        {
            Strip = new ContextMenuStrip();

            Problem = Add("Last problem...");
            afterProblem = Separate();

            Toggle = Add("Toggle PAC");
            FollowVpn = Add("Follow VPN automatically");
            Invert = Add("Invert (PAC on when VPN is off)");
            Separate();
            SetUrl = Add("Set PAC URL...");
            SetAdapter = Add("Set VPN adapter...");
            AutoStart = Add("Start with Windows");
            OpenLog = Add("Open log");
            Separate();
            Exit = Add("Exit");
        }

        public ContextMenuStrip Strip { get; private set; }

        public ToolStripMenuItem Problem { get; private set; }
        public ToolStripMenuItem Toggle { get; private set; }
        public ToolStripMenuItem FollowVpn { get; private set; }
        public ToolStripMenuItem Invert { get; private set; }
        public ToolStripMenuItem SetUrl { get; private set; }
        public ToolStripMenuItem SetAdapter { get; private set; }
        public ToolStripMenuItem AutoStart { get; private set; }
        public ToolStripMenuItem OpenLog { get; private set; }
        public ToolStripMenuItem Exit { get; private set; }

        public void Reflect(bool pacEnabled, Settings settings, bool autoStartEnabled,
                            string problemLine)
        {
            bool hasProblem = problemLine.Length > 0;
            Problem.Visible = hasProblem;
            afterProblem.Visible = hasProblem;
            if (hasProblem) Problem.Text = problemLine;

            Toggle.Text = pacEnabled ? "Turn PAC off" : "Turn PAC on";
            FollowVpn.Checked = settings.FollowVpn;
            Invert.Checked = settings.Inverted;
            Invert.Enabled = settings.FollowVpn;
            AutoStart.Checked = autoStartEnabled;
        }

        // NotifyIcon does not own the strip it is given, so nobody else frees it.
        public void Dispose()
        {
            Strip.Dispose();
        }

        private ToolStripMenuItem Add(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            Strip.Items.Add(item);
            return item;
        }

        private ToolStripSeparator Separate()
        {
            ToolStripSeparator separator = new ToolStripSeparator();
            Strip.Items.Add(separator);
            return separator;
        }
    }
}
