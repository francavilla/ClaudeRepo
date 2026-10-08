using System.Windows;
using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.UI.Wpf
{
    internal sealed class WpfDialogService : IDialogService
    {
        public bool Confirm(string title, string message)
        {
            return MessageBox.Show(Owner(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(Owner(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static Window Owner() => Application.Current?.MainWindow;
    }
}
