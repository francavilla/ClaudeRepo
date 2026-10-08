using System;
using System.Windows.Forms;

namespace Gestionale
{
    internal static class Program
    {
        public static string Utente;

        [STAThread]
        private static void Main()
        {
            Application.Run(new OrdiniForm());
        }
    }
}
