using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using PasswordGen.Core.Generation;
using PasswordGen.Core.Randomness;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Settings;
using PasswordGen.Services;
using PasswordGen.ViewModels;

namespace PasswordGen
{
    public partial class App : Application
    {
        private static readonly TimeSpan ClipboardLifetime = TimeSpan.FromSeconds(30);

        private SecureRandom _random;
        private ISecretClipboard _clipboard;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            var store = new SettingsStore(SettingsStore.DefaultPath);

            // Avvio automatico con Windows (/promemoria): la finestra si apre solo se la password sta per scadere.
            var fromStartup = e.Args.Any(a => string.Equals(a, StartupRegistration.ReminderArgument, StringComparison.OrdinalIgnoreCase));
            if (fromStartup)
            {
                var settings = store.Load();
                var state = ChangeReminder.Evaluate(settings.ReminderEnabled, settings.LastChangeDate, settings.ValidityDays, settings.WarnDays, DateTime.Today);
                if (!state.ShouldAlert)
                {
                    Shutdown();
                    return;
                }
            }

            // Composition root: dipendenze create a mano, l'applicazione è piccola e non serve un container DI.
            _random = new SecureRandom();
            _clipboard = new SecretClipboard(ClipboardLifetime);
            var generator = new PasswordGenerator(_random, WordList.LoadItalian());
            var viewModel = new MainViewModel(generator, store, _clipboard, new StartupRegistration(), () => DateTime.Today);

            var window = new MainWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // La password copiata non deve restare negli appunti dopo la chiusura.
            if (_clipboard != null)
            {
                _clipboard.ClearIfPending();
            }

            if (_random != null)
            {
                _random.Dispose();
            }

            base.OnExit(e);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "PasswordGen - errore imprevisto", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
