using System;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopAppTemplate.UI.WinForms.Components
{
    /// <summary>Pulsante piatto con il colore d'accento.</summary>
    internal sealed class AccentButton : Button
    {
        public AccentButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Palette.AccentHover;
            FlatAppearance.MouseDownBackColor = Palette.AccentHover;
            ForeColor = Color.White;
            Font = Palette.Ui(10f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
            UpdateColors();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateColors();
        }

        private void UpdateColors()
        {
            BackColor = Enabled ? Palette.Accent : Palette.AccentDisabled;
            ForeColor = Color.White;
        }
    }
}
