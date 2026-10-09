using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using PasswordGen.Core.Security;

namespace PasswordGen.Services
{
    public sealed class DialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return Show(() => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes);
        }

        public string PickFile(string title, string filter)
        {
            return Show(() =>
            {
                var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true, Multiselect = false };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            });
        }

        public string PickSaveFile(string title, string filter, string fileName, bool confirmOverwrite)
        {
            return Show(() =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = title,
                    Filter = filter,
                    FileName = fileName,
                    OverwritePrompt = confirmOverwrite,
                    CheckPathExists = true,
                    AddExtension = true
                };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            });
        }

        public string AskPassphrase(string title, string message, bool confirm)
        {
            return Show(() => PassphraseDialog.Ask(title, message, confirm));
        }

        public string AskText(string title, string message, string initial)
        {
            return Show(() => SecretDialogs.AskText(title, message, initial));
        }

        public int Choose(string title, IReadOnlyList<string> options)
        {
            return Show(() => SecretDialogs.Choose(title, options));
        }

        public string AskNewSecret(CredentialKind kind)
        {
            return Show(() => SecretDialogs.AskNew(kind));
        }

        public string AskSecret(string title, string message)
        {
            return Show(() => SecretDialogs.Ask(title, message));
        }

        /// <summary>Mentre la finestra è aperta la finestra principale perde il fuoco: non deve contare come uscita ai fini del blocco.</summary>
        private static T Show<T>(Func<T> dialog)
        {
            ExternalActivity.Enter();
            try
            {
                return dialog();
            }
            finally
            {
                ExternalActivity.Leave();
            }
        }
    }
}
