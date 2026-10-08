using System;
using System.Windows;
using System.Windows.Threading;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Features.Shell;
using DesktopAppTemplate.UI.Wpf.Views;

namespace DesktopAppTemplate.UI.Wpf
{
    /// <summary>Avvia l'applicazione WPF (nessun App.xaml: l'<see cref="Application"/> si crea qui, nel composition root).</summary>
    internal sealed class WpfShell : IUiShell
    {
        private const string ThemeUri = "pack://application:,,,/DesktopAppTemplate.UI.Wpf;component/Themes/Theme.xaml";

        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogs;

        public WpfShell(MainViewModel mainViewModel, IDialogService dialogs)
        {
            _mainViewModel = mainViewModel;
            _dialogs = dialogs;
        }

        public int Run()
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ThemeUri, UriKind.Absolute) });
            app.DispatcherUnhandledException += OnUnhandledException;

            var window = new MainWindow { DataContext = _mainViewModel };
            return app.Run(window);
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _dialogs.ShowError("Errore imprevisto", e.Exception.Message);
            e.Handled = true;
        }
    }
}
