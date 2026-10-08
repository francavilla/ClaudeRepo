using System.Drawing;

namespace DesktopAppTemplate.UI.WinForms
{
    /// <summary>Colori e font condivisi: stessa tavolozza del tema WPF per un aspetto coerente.</summary>
    internal static class Palette
    {
        public static readonly Color Background = ColorTranslator.FromHtml("#F3F4F6");
        public static readonly Color Surface = Color.White;
        public static readonly Color Sidebar = ColorTranslator.FromHtml("#111827");
        public static readonly Color SidebarHover = ColorTranslator.FromHtml("#1F2937");
        public static readonly Color SidebarSelected = ColorTranslator.FromHtml("#312E81");
        public static readonly Color SidebarText = ColorTranslator.FromHtml("#D1D5DB");
        public static readonly Color Accent = ColorTranslator.FromHtml("#4F46E5");
        public static readonly Color AccentHover = ColorTranslator.FromHtml("#4338CA");
        public static readonly Color AccentDisabled = ColorTranslator.FromHtml("#A5B4FC");
        public static readonly Color AccentSoft = ColorTranslator.FromHtml("#EEF2FF");
        public static readonly Color Text = ColorTranslator.FromHtml("#111827");
        public static readonly Color Muted = ColorTranslator.FromHtml("#6B7280");
        public static readonly Color Line = ColorTranslator.FromHtml("#E5E7EB");
        public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");
        public static readonly Color DangerSoft = ColorTranslator.FromHtml("#FEE2E2");

        public static Font Ui(float size, FontStyle style = FontStyle.Regular) => new Font("Segoe UI", size, style);

        /// <summary>Font con le icone di sistema (Windows 10+).</summary>
        public static Font Icons(float size) => new Font("Segoe MDL2 Assets", size);
    }
}
