using System.Windows;
using Microsoft.Win32;

namespace PasswordGen.Services
{
    public sealed class DialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public string PickFile(string title, string filter)
        {
            var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true, Multiselect = false };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
