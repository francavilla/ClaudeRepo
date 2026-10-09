using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Randomness;
using PasswordGen.Core.Security;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using PasswordGen.Mobile.Services;
using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        // Composition root: dipendenze create a mano, l'app è piccola e non serve un container DI.
        var words = WordList.LoadItalian();
        var generator = new PasswordGenerator(new SecureRandom(), words);
        var settings = new SettingsStore(Path.Combine(FileSystem.AppDataDirectory, "settings.json"));

        // Lo storico è cifrato con una chiave che sta nel Keystore di Android (SecureStorage); senza chiave resta disattivato.
        var key = HistoryKey.GetOrCreate();
        var history = key == null
            ? null
            : new HistoryStore(Path.Combine(FileSystem.AppDataDirectory, "history.dat"), new AesHmacProtector(key));

        var security = new AndroidSecurityService();
        var saved = settings.Load();

        // PIN o password dell'app: hash in un file cifrato con la stessa chiave del Keystore; senza chiave restano impronta e PIN del telefono.
        var credentials = key == null
            ? null
            : new LockCredentialManager(new LockCredentialStore(Path.Combine(FileSystem.AppDataDirectory, "lock.dat"), new AesHmacProtector(key)));

        // Se il blocco schermo è stato tolto e non c'è un PIN o una password dell'app, non si potrebbe sbloccare: il blocco resta spento.
        var lockState = new AppLockState(TimeSpan.FromSeconds(saved.LockGraceSeconds))
        {
            Enabled = saved.LockEnabled && (security.IsAvailable || (credentials != null && credentials.HasCredential)),
        };

        TabbedPage tabs = null;
        var appLock = new AppLockController(lockState, security, credentials, () => tabs);

        var viewModel = new MainViewModel(
            generator, words, settings, history, new SecretClipboard(TimeSpan.FromSeconds(30)),
            new DialogService(), new AndroidReminderScheduler(), new WordFileService(), security, appLock,
            key == null ? null : new SyncPassphraseStore(Path.Combine(FileSystem.AppDataDirectory, "sync.key"), new AesHmacProtector(key)),
            new AndroidDocumentService(),
            new GoogleDriveService(key == null ? null : new SyncPassphraseStore(Path.Combine(FileSystem.AppDataDirectory, "google.key"), new AesHmacProtector(key))));
        viewModel.RestoreReminder();

        tabs = new TabbedPage();
        tabs.Children.Add(new MainPage(viewModel));
        tabs.Children.Add(new HistoryPage(viewModel));
        tabs.Children.Add(new SettingsPage(viewModel));

        // All'avvio l'app parte bloccata (se il blocco è attivo): la schermata di blocco compare appena la pagina è visibile.
        var started = false;
        tabs.Appearing += (sender, args) =>
        {
            if (!started)
            {
                started = true;
                appLock.Start();
                _ = viewModel.AutoSyncAsync();
            }
        };

        var window = new Window(tabs);
        // In secondo piano: via la password attuale, password dello storico di nuovo mascherate, parte il conto del blocco.
        window.Stopped += (sender, args) =>
        {
            viewModel.ClearSensitive();
            appLock.Backgrounded();
        };
        window.Resumed += (sender, args) =>
        {
            appLock.Resumed();
            _ = viewModel.SyncIfIdleAsync(TimeSpan.FromSeconds(20));
        };

        // Con l'app in primo piano si controlla ogni due minuti se l'altro dispositivo ha cambiato qualcosa (per esempio azzerato lo storico).
        var syncTimer = Dispatcher.CreateTimer();
        syncTimer.Interval = TimeSpan.FromMinutes(2);
        syncTimer.Tick += (sender, args) => _ = viewModel.SyncIfIdleAsync(TimeSpan.FromSeconds(60));
        syncTimer.Start();
        return window;
    }
}
