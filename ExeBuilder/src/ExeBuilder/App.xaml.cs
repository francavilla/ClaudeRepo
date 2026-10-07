using System.Windows;
using System.Windows.Threading;
using ExeBuilder.Core.Analysis;
using ExeBuilder.Core.Execution;
using ExeBuilder.Core.Planning;
using ExeBuilder.Core.Toolchain;
using ExeBuilder.Services;
using ExeBuilder.ViewModels;

namespace ExeBuilder
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Composition root: dipendenze create a mano, l'applicazione è piccola e non serve un container DI.
            var viewModel = new MainViewModel(
                new ProjectAnalyzer(),
                new SolutionParser(),
                new BuildPlanner(),
                new BuildService(new ProcessRunner()),
                new ToolchainLocator(),
                new DialogService());

            var window = new MainWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();

            // Argomento opzionale da riga di comando: ExeBuilder.exe "C:\percorso\Progetto.csproj"
            if (e.Args.Length > 0)
            {
                viewModel.InputPath = e.Args[0];
            }

            var ignored = viewModel.InitializeAsync();
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "ExeBuilder - errore imprevisto", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
