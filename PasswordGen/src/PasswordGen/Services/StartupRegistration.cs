using System;
using System.Reflection;
using Microsoft.Win32;

namespace PasswordGen.Services
{
    /// <summary>
    /// Registra l'applicazione nella chiave Run dell'utente corrente (nessun permesso di amministratore).
    /// All'avvio automatico viene passato <see cref="ReminderArgument"/>: la finestra si apre solo se la password è in scadenza.
    /// </summary>
    public sealed class StartupRegistration : IStartupRegistration
    {
        public const string ReminderArgument = "/promemoria";

        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "PasswordGen";

        public bool IsEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return key != null && key.GetValue(ValueName) != null;
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("Impossibile aprire la chiave di avvio automatico di Windows.");
                }

                if (enabled)
                {
                    var exe = Assembly.GetEntryAssembly().Location;
                    key.SetValue(ValueName, "\"" + exe + "\" " + ReminderArgument);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }
    }
}
