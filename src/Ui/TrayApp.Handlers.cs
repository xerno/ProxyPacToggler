using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ProxyPacToggler.Core;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Ui
{
    internal sealed partial class TrayApp
    {
        private void OnTrayClicked(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) OnToggleClicked(sender, e);
        }

        private void OnToggleClicked(object sender, EventArgs e)
        {
            Guarded("switching the PAC script", delegate
            {
                if (!controller.IsPacEnabled && !EnsurePacUrl()) return;

                Result result = controller.Toggle();
                if (!result.Succeeded) Report("Could not switch the PAC script", result.Error);
                Refresh();
            });
        }

        private void OnFollowVpnClicked(object sender, EventArgs e)
        {
            Guarded("changing the mode", delegate
            {
                Save(delegate(Settings s) { s.FollowVpn = !s.FollowVpn; });
                log.Write("mode: " + (controller.Settings.FollowVpn ? "auto" : "manual"));
                Synchronise(true, "mode");
            });
        }

        private void OnInvertClicked(object sender, EventArgs e)
        {
            Guarded("changing the inverted setting", delegate
            {
                Save(delegate(Settings s) { s.Inverted = !s.Inverted; });
                log.Write("inverted: " + controller.Settings.Inverted);
                Synchronise(true, "invert");
            });
        }

        private void OnSetUrlClicked(object sender, EventArgs e)
        {
            Guarded("setting the PAC URL", delegate
            {
                if (!PromptForPacUrl(controller.Settings.PacUrl)) return;

                if (controller.IsPacEnabled)
                {
                    Result result = controller.Enable(controller.Settings.PacUrl);
                    if (!result.Succeeded) Report("Could not apply the new PAC URL", result.Error);
                }
                Refresh();
            });
        }

        private void OnSetAdapterClicked(object sender, EventArgs e)
        {
            Guarded("setting the VPN adapter", delegate
            {
                string entered = InputDialog.Ask(
                    "Match VPN adapter by name or description containing:",
                    controller.Settings.AdapterMatch);
                if (string.IsNullOrEmpty(entered)) return;

                Save(delegate(Settings s) { s.AdapterMatch = entered; });
                log.Write("adapter match: " + entered);

                controller.ForgetVpnState();
                Synchronise(true, "adapter");
            });
        }

        private void OnAutoStartClicked(object sender, EventArgs e)
        {
            Guarded("changing the autostart setting", delegate
            {
                Result result = autoStart.Set(!autoStart.IsEnabled);
                if (!result.Succeeded) Report("Could not change the autostart setting", result.Error);
                Refresh();
            });
        }

        private void OnOpenLogClicked(object sender, EventArgs e)
        {
            Guarded("opening the log", OpenLog);
        }

        // The toast may be long gone, so the details stay reachable from the menu.
        private void OnProblemClicked(object sender, EventArgs e)
        {
            Guarded("showing the last problem", delegate
            {
                DialogResult answer = MessageBox.Show(
                    problems.FullText() + Environment.NewLine + Environment.NewLine
                        + "Open the log for the full history?",
                    "ProxyPacToggler", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                problems.Acknowledge();
                if (answer == DialogResult.Yes) OpenLog();
                Refresh();
            });
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            if (!ConfirmExitWithPacEnabled()) return;

            log.Write("exit requested by user");

            // A deliberate quit must not be undone by the watcher.
            ProcessGuard.RequestStop();

            tray.Visible = false;
            Application.Exit();
        }

        // Quitting with it on leaves a proxy that nothing will switch off.
        private bool ConfirmExitWithPacEnabled()
        {
            if (!controller.IsPacEnabled) return true;

            DialogResult answer = MessageBox.Show(
                "The PAC script is still on and nothing will switch it off once this "
                    + "closes, so browsing will break when the VPN drops."
                    + Environment.NewLine + Environment.NewLine
                    + "Switch it off before quitting?",
                "ProxyPacToggler", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            if (answer == DialogResult.Cancel) return false;

            if (answer == DialogResult.Yes)
            {
                Result cleared = controller.Disable();
                if (!cleared.Succeeded)
                {
                    Report("Could not switch the PAC script off", cleared.Error);
                    return false;
                }
            }
            else
            {
                log.Write("quitting with the PAC script left on, at the user's request");
            }

            return true;
        }

        private void OpenLog()
        {
            if (!File.Exists(log.Location)) log.Write("log opened");
            // The returned Process owns a handle, and the log can be opened repeatedly.
            using (Process.Start("notepad.exe", log.Location)) { }
        }

        private bool EnsurePacUrl()
        {
            return controller.Settings.PacUrl.Trim().Length > 0 || PromptForPacUrl("");
        }

        // A dialog from a network event would block the loop and surprise the user.
        private bool PromptForPacUrl(string initial)
        {
            string candidate = initial;
            while (true)
            {
                string entered = InputDialog.Ask("PAC script URL:", candidate);
                if (string.IsNullOrEmpty(entered)) return false;

                Result validated = PacUrlValidator.Validate(entered);
                if (validated.Succeeded)
                {
                    Save(delegate(Settings s) { s.PacUrl = entered; });
                    log.Write("PAC URL: " + entered);
                    return true;
                }

                MessageBox.Show("That PAC URL cannot be used: " + validated.Error + ".",
                                "ProxyPacToggler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                candidate = entered;
            }
        }

        private void Save(Action<Settings> change)
        {
            Result saved = controller.UpdateSettings(change);
            if (!saved.Succeeded) Report("Your change was applied but not saved", saved.Error);
        }
    }
}
