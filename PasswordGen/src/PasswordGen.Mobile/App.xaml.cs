using Microsoft.Maui.Storage;
using PasswordGen.Core.Generation;
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
        var viewModel = new MainViewModel(generator, settings, new SecretClipboard(TimeSpan.FromSeconds(30)));

        return new Window(new MainPage(viewModel));
    }
}
