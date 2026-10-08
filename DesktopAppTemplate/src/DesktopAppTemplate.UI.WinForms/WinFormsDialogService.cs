using System.Windows.Forms;
using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.UI.WinForms
{
    internal sealed class WinFormsDialogService : IDialogService
    {
        public bool Confirm(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
