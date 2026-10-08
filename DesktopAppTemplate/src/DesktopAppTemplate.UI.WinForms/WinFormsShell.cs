using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Features.Shell;

namespace DesktopAppTemplate.UI.WinForms
{
    /// <summary>Avvia l'applicazione Windows Forms.</summary>
    internal sealed class WinFormsShell : IUiShell
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogs;

        public WinFormsShell(MainViewModel mainViewModel, IDialogService dialogs)
        {
            _mainViewModel = mainViewModel;
            _dialogs = dialogs;
        }

        public int Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += OnThreadException;

            using (var form = new MainForm(_mainViewModel))
            {
                form.Icon = LoadApplicationIcon();
                // Gli errori dei comandi sono già gestiti dai view model.
                form.Shown += async (s, e) => await _mainViewModel.InitializeAsync();
                Application.Run(form);
            }

            return 0;
        }

        /// <summary>Usa l'icona dell'.exe (la stessa della finestra WPF). Se non disponibile resta quella predefinita.</summary>
        private static Icon LoadApplicationIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _dialogs.ShowError("Errore imprevisto", e.Exception.Message);
        }
    }
}
