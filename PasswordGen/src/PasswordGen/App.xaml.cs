using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Randomness;
using PasswordGen.Core.Security;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
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
            var builtinWords = WordList.LoadItalian();
            var generator = new PasswordGenerator(_random, builtinWords);

            // Blocco con Windows Hello: parte già bloccato (se attivo) finché la finestra non è pronta e l'utente non si autentica.
            MainWindow window = null;
            var saved = store.Load();
            var lockState = new AppLockState(TimeSpan.FromSeconds(saved.LockGraceSeconds)) { Enabled = saved.LockEnabled };
            // PIN o password dell'app: hash in un file cifrato con DPAPI (lock.dat), accanto alle impostazioni.
            var dataDirectory = System.IO.Path.GetDirectoryName(SettingsStore.DefaultPath);
            var credentials = new LockCredentialManager(new LockCredentialStore(System.IO.Path.Combine(dataDirectory, "lock.dat"), new DpapiProtector()));
            var appLock = new AppLockController(
                lockState, new WindowsHelloService(), () => window == null ? IntPtr.Zero : new WindowInteropHelper(window).Handle, credentials);

            var viewModel = new MainViewModel(generator, builtinWords, store, _clipboard, new StartupRegistration(),
                new HistoryStore(HistoryStore.DefaultPath, new DpapiProtector()), new DialogService(), appLock,
                new SyncPassphraseStore(System.IO.Path.Combine(dataDirectory, "sync.key"), new DpapiProtector()),
                () => DateTime.Today);

            window = new MainWindow { DataContext = viewModel };
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
