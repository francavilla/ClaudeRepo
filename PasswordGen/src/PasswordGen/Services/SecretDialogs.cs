using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PasswordGen.Core.Security;

namespace PasswordGen.Services
{
    /// <summary>Piccole finestre per scegliere una voce o per inserire un PIN o una password, create da codice.</summary>
    internal static class SecretDialogs
    {
        /// <summary>Scelta tra più voci (pulsanti di opzione); restituisce l'indice, o -1 se l'utente annulla.</summary>
        public static int Choose(string title, IReadOnlyList<string> options)
        {
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });

            var buttons = new List<RadioButton>();
            for (var i = 0; i < options.Count; i++)
            {
                var radio = new RadioButton { Content = options[i], Margin = new Thickness(0, 0, 0, 8), IsChecked = i == 0 };
                buttons.Add(radio);
                panel.Children.Add(radio);
            }

            var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 8, 8, 0) };
            var cancel = new Button { Content = "Annulla", IsCancel = true, MinWidth = 80, Margin = new Thickness(0, 8, 0, 0) };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row.Children.Add(ok);
            row.Children.Add(cancel);
            panel.Children.Add(row);

            var window = CreateWindow("PasswordGen", panel);
            var chosen = -1;
            ok.Click += (s, e) =>
            {
                chosen = buttons.FindIndex(b => b.IsChecked == true);
                window.DialogResult = true;
            };

            window.ShowDialog();
            return chosen;
        }

        /// <summary>Chiede un nuovo PIN o una nuova password (due volte, con le regole del Core); null se l'utente annulla.</summary>
        public static string AskNew(CredentialKind kind)
        {
            var isPin = kind == CredentialKind.Pin;
            var hint = isPin
                ? "Da " + LockCredential.PinMinLength + " a " + LockCredential.PinMaxLength + " cifre. Più è lungo, più è sicuro: meglio 6 cifre o più."
                : "Da " + LockCredential.PasswordMinLength + " a " + LockCredential.PasswordMaxLength + " caratteri. Non serve che sia la stessa di nessun altro posto.";

            var first = NewBox();
            var second = NewBox();
            var error = ErrorText();
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = isPin ? "Scegli un PIN" : "Scegli una password", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            panel.Children.Add(new TextBlock { Text = hint, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
            panel.Children.Add(new TextBlock { Text = isPin ? "Nuovo PIN" : "Nuova password", Margin = new Thickness(0, 0, 0, 4) });
            panel.Children.Add(first);
            panel.Children.Add(new TextBlock { Text = isPin ? "Ripeti il PIN" : "Ripeti la password", Margin = new Thickness(0, 0, 0, 4) });
            panel.Children.Add(second);
            panel.Children.Add(error);

            var ok = new Button { Content = "Salva", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Annulla", IsCancel = true, MinWidth = 80 };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row.Children.Add(ok);
            row.Children.Add(cancel);
            panel.Children.Add(row);

            var window = CreateWindow(isPin ? "Imposta il PIN" : "Imposta la password", panel);
            string result = null;
            ok.Click += (s, e) =>
            {
                var problem = LockCredential.Validate(kind, first.Password);
                if (problem == null && first.Password != second.Password)
                {
                    problem = isPin ? "I due PIN non coincidono." : "Le due password non coincidono.";
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

        /// <summary>Chiede il PIN o la password già impostati (per confermare un'azione); null se l'utente annulla.</summary>
        public static string Ask(string title, string message)
        {
            var box = NewBox();
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
            panel.Children.Add(box);

            var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Annulla", IsCancel = true, MinWidth = 80 };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row.Children.Add(ok);
            row.Children.Add(cancel);
            panel.Children.Add(row);

            var window = CreateWindow(title, panel);
            string result = null;
            ok.Click += (s, e) =>
            {
                result = box.Password;
                window.DialogResult = true;
            };

            box.Focus();
            window.ShowDialog();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private static PasswordBox NewBox()
        {
            return new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(6, 5, 6, 5) };
        }

        private static TextBlock ErrorText()
        {
            return new TextBlock { Foreground = System.Windows.Media.Brushes.Firebrick, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
        }

        private static Window CreateWindow(string title, UIElement content)
        {
            return new Window
            {
                Title = title,
                Content = content,
                Width = 420,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Owner = Application.Current == null ? null : Application.Current.MainWindow
            };
        }
    }
}
