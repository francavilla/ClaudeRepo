using System.Drawing;
using System.Windows.Forms;

namespace DesktopAppTemplate.UI.WinForms.Components
{
    /// <summary>Pannello bianco con bordo sottile, equivalente della "card" del tema WPF.</summary>
    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            BackColor = Palette.Surface;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Palette.Line))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
