using System.Windows;
using System.Windows.Controls;
using PasswordGen.Core.Sync;

namespace PasswordGen.Services
{
    /// <summary>Piccola finestra per inserire una frase segreta, creata da codice (niente XAML da mantenere).</summary>
    internal static class PassphraseDialog
    {
        public static string Ask(string title, string message, bool confirm)
        {
            var first = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(6, 5, 6, 5) };
            var second = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(6, 5, 6, 5) };
            var error = new TextBlock { Foreground = System.Windows.Media.Brushes.Firebrick, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Annulla", IsCancel = true, MinWidth = 80 };

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
            panel.Children.Add(new TextBlock { Text = "Frase segreta", Margin = new Thickness(0, 0, 0, 4) });
            panel.Children.Add(first);
            if (confirm)
            {
                panel.Children.Add(new TextBlock { Text = "Ripeti la frase segreta", Margin = new Thickness(0, 0, 0, 4) });
                panel.Children.Add(second);
            }

            panel.Children.Add(error);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);

            var window = new Window
            {
                Title = title,
                Content = panel,
                Width = 420,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Owner = Application.Current == null ? null : Application.Current.MainWindow
            };

            string result = null;
            ok.Click += (s, e) =>
            {
                var problem = ExchangeFile.ValidatePassphrase(first.Password);
                if (problem == null && confirm && first.Password != second.Password)
                {
                    problem = "Le due frasi non coincidono.";
                }

                if (problem != null)
                {
                    error.Text = problem;
                    return;
                }

                result = first.Password;
                window.DialogResult = true;
            };

            first.Focus();
            window.ShowDialog();
            return result;
        }
    }
}
