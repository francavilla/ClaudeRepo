using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using SolutionDoctor.App.Services;
using SolutionDoctor.Presentation.Services;
using SolutionDoctor.Presentation.ViewModels;

namespace SolutionDoctor.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Composition root: dipendenze create a mano, l'applicazione è piccola e non serve un container DI.
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolutionDoctor", "settings.txt");
            var viewModel = new MainViewModel(new CoreAnalyzer(), new DialogService(), new FileSettingsStore(settingsPath), AppInfo.Version);
            viewModel.LoadSettings();

            var window = new MainWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();

            // Argomento opzionale: SolutionDoctor.App.exe "C:\percorso\Soluzione.sln" analizza subito.
            if (e.Args.Length > 0)
            {
                viewModel.InputPath = e.Args[0];
                viewModel.AnalyzeCommand.Execute(null);
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "SolutionDoctor - errore imprevisto", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
