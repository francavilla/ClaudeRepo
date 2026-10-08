using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DesktopAppTemplate.Host
{
    /// <summary>
    /// Mostra un testo da riga di comando anche se l'eseguibile è di tipo "Windows" (senza console):
    /// se è stato avviato da un prompt scrive lì, altrimenti (es. collegamento con argomenti) usa una finestra di messaggio.
    /// </summary>
    internal static class ConsoleOutput
    {
        private const int AttachParentProcess = -1;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareWrite = 0x2;
        private const uint OpenExisting = 3;
        private const uint MessageBoxInformation = 0x40;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
            IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        public static void Show(string title, string text)
        {
            if (TryWriteToParentConsole(text))
                return;

            MessageBoxW(IntPtr.Zero, text, title, MessageBoxInformation);
        }

        private static bool TryWriteToParentConsole(string text)
        {
            if (!AttachConsole(AttachParentProcess))
                return false;

            using (var handle = CreateFile("CONOUT$", GenericWrite, FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid)
                    return false;

                using (var stream = new FileStream(handle, FileAccess.Write))
                using (var writer = new StreamWriter(stream, Console.OutputEncoding))
                {
                    writer.WriteLine();
                    writer.WriteLine(text);
                }
            }

            return true;
        }
    }
}
