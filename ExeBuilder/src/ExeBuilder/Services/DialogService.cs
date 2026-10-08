using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ExeBuilder.Services
{
    public sealed class DialogService : IDialogService
    {
        public string PickProjectOrSolution(string initialPath)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleziona un progetto o una solution",
                Filter = "Progetti e solution|*.sln;*.slnx;*.csproj;*.vbproj;*.fsproj|Solution (*.sln, *.slnx)|*.sln;*.slnx|Progetti (*.csproj, *.vbproj, *.fsproj)|*.csproj;*.vbproj;*.fsproj|Tutti i file|*.*",
                CheckFileExists = true
            };

            var directory = ExistingDirectory(initialPath);
            if (directory != null)
            {
                dialog.InitialDirectory = directory;
            }

            return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
        }

        public string PickFolder(string initialPath)
        {
            return FolderPicker.Show(Application.Current.MainWindow, "Seleziona la cartella di output", ExistingDirectory(initialPath));
        }

        public void ShowError(string message)
        {
            MessageBox.Show(Application.Current.MainWindow, message, "ExeBuilder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
            }
        }

        public void CopyToClipboard(string text)
        {
            Clipboard.SetText(text ?? string.Empty);
        }

        private static string ExistingDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    return path;
                }

                var parent = Path.GetDirectoryName(path);
                return parent != null && Directory.Exists(parent) ? parent : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
