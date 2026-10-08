using System;
using System.Windows;
using System.Windows.Threading;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Features.Shell;
using Microsoft.Extensions.Logging;
using DesktopAppTemplate.UI.Wpf.Views;

namespace DesktopAppTemplate.UI.Wpf
{
    /// <summary>Avvia l'applicazione WPF (nessun App.xaml: l'<see cref="Application"/> si crea qui, nel composition root).</summary>
    internal sealed class WpfShell : IUiShell
    {
        private const string ThemeUri = "pack://application:,,,/DesktopAppTemplate.UI.Wpf;component/Themes/Theme.xaml";

        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogs;
        private readonly ILogger<WpfShell> _logger;

        public WpfShell(MainViewModel mainViewModel, IDialogService dialogs, ILogger<WpfShell> logger)
        {
            _mainViewModel = mainViewModel;
            _dialogs = dialogs;
            _logger = logger;
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
            _logger.LogError(e.Exception, "Errore non gestito nella finestra WPF");
            _dialogs.ShowError("Errore imprevisto", e.Exception.Message);
            e.Handled = true;
        }
    }
}
