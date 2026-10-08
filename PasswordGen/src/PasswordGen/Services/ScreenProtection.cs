using System;
using System.Runtime.InteropServices;

namespace PasswordGen.Services
{
    /// <summary>Esclude la finestra dalle catture dello schermo (screenshot, registrazioni, condivisione schermo). Windows 10 2004 o successivo.</summary>
    public static class ScreenProtection
    {
        private const uint WdaNone = 0x0;
        private const uint WdaExcludeFromCapture = 0x11;

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        public static void Apply(IntPtr window, bool blocked)
        {
            if (window == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SetWindowDisplayAffinity(window, blocked ? WdaExcludeFromCapture : WdaNone);
            }
            catch (Exception)
            {
                // Funzione di sicurezza aggiuntiva: se il sistema non la supporta non cambia nulla.
            }
        }
    }
}
