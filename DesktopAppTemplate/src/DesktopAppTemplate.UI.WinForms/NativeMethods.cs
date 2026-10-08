using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopAppTemplate.UI.WinForms
{
    internal static class NativeMethods
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        /// <summary>Mostra un testo suggerimento (placeholder) nella casella vuota. .NET Framework non lo offre di serie.</summary>
        public static void SetCueBanner(TextBox textBox, string text)
        {
            if (textBox.IsHandleCreated)
                SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
            else
                textBox.HandleCreated += (s, e) => SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
        }
    }
}
