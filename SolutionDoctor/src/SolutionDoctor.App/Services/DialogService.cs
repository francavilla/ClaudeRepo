using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using SolutionDoctor.Presentation.Services;

namespace SolutionDoctor.App.Services
{
    /// <summary>Finestre di dialogo, appunti e shell di Windows; è l'unico punto che le tocca.</summary>
    public sealed class DialogService : IDialogService
    {
        private const string Title = "SolutionDoctor";

        public string PickSolutionOrProject(string initialPath)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Scegli una solution o un progetto C#",
                Filter = "Solution e progetti C# (*.sln;*.csproj)|*.sln;*.csproj|Tutti i file (*.*)|*.*",
                CheckFileExists = true
            };

            if (File.Exists(initialPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                dialog.FileName = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string PickFolder(string initialPath)
        {
            var dialog = new OpenFolderDialog { Title = "Scegli la cartella da analizzare" };
            if (Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        public string PickReportFile(string suggestedFileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Salva il report",
                Filter = "Documento Markdown (*.md)|*.md|Tutti i file (*.*)|*.*",
                FileName = suggestedFileName,
                DefaultExt = ".md",
                AddExtension = true,
                OverwritePrompt = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                ShowError("Il file non esiste più:" + System.Environment.NewLine + path);
                return;
            }

            Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public void RevealInFolder(string path)
        {
            if (File.Exists(path))
            {
                Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\""));
            }
            else if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                OpenFolder(Path.GetDirectoryName(path));
            }
            else
            {
                ShowError("La cartella non esiste più:" + System.Environment.NewLine + path);
            }
        }

        public void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                ShowError("La cartella non esiste più:" + System.Environment.NewLine + path);
                return;
            }

            Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (ExternalException)
            {
                // Gli appunti possono essere occupati da un altro programma.
                ShowError("Impossibile accedere agli appunti: riprova.");
            }
        }

        private void Start(ProcessStartInfo info)
        {
            try
            {
                Process.Start(info);
            }
            catch (Win32Exception ex)
            {
                ShowError("Impossibile aprire l'elemento: " + ex.Message);
            }
        }
    }
}
