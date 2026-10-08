using System;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopAppTemplate.UI.WinForms.Components
{
    /// <summary>Voce del menu laterale: icona (Segoe MDL2) + testo, con stato di hover e selezione.</summary>
    internal sealed class NavButton : Control
    {
        private bool _hover;
        private bool _selected;
        private readonly Font _iconFont = Palette.Icons(13f);

        public NavButton(string glyph, string caption)
        {
            Glyph = glyph;
            Text = caption;
            Font = Palette.Ui(10.5f);
            Height = 44;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            SetStyle(ControlStyles.Selectable, false);
        }

        public string Glyph { get; }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Palette.Sidebar);

            var back = _selected ? Palette.SidebarSelected : _hover ? Palette.SidebarHover : Palette.Sidebar;
            var area = new Rectangle(8, 2, Width - 16, Height - 4);
            using (var brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, area);

            var fore = _selected ? Color.White : Palette.SidebarText;
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(e.Graphics, Glyph, _iconFont, new Rectangle(area.X + 14, area.Y, 28, area.Height), fore, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(area.X + 46, area.Y, area.Width - 54, area.Height), fore, flags);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _iconFont.Dispose();
            base.Dispose(disposing);
        }
    }
}
