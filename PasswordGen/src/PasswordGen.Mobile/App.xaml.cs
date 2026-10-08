using Microsoft.Maui.Storage;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Randomness;
using PasswordGen.Core.Settings;
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

        var viewModel = new MainViewModel(
            generator, settings, history, new SecretClipboard(TimeSpan.FromSeconds(30)), new DialogService(), new AndroidReminderScheduler());
        viewModel.RestoreReminder();

        var tabs = new TabbedPage();
        tabs.Children.Add(new MainPage(viewModel));
        tabs.Children.Add(new HistoryPage(viewModel));

        var window = new Window(tabs);
        // Quando l'app va in secondo piano: via la password attuale e password dello storico di nuovo mascherate.
        window.Stopped += (sender, args) => viewModel.ClearSensitive();
        return window;
    }
}
