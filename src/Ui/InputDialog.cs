using System.Drawing;
using System.Windows.Forms;

namespace ProxyPacToggler.Ui
{
    internal sealed class InputDialog : Form
    {
        private readonly TextBox input = new TextBox();

        private InputDialog(string prompt, string initial)
        {
            Text = "ProxyPacToggler";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 110);

            Label label = new Label();
            label.Text = prompt;
            label.SetBounds(12, 12, 436, 20);

            input.Text = initial;
            input.SetBounds(12, 36, 436, 22);

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(280, 72, 80, 26);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(368, 72, 80, 26);

            Controls.AddRange(new Control[] { label, input, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        // Returns null when the user cancels, so an empty answer stays distinguishable.
        public static string Ask(string prompt, string initial)
        {
            using (InputDialog dialog = new InputDialog(prompt, initial))
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.input.Text.Trim() : null;
            }
        }
    }
}
