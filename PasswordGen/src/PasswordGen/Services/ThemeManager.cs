using System;
using System.Windows;
using Microsoft.Win32;

namespace PasswordGen.Services
{
    /// <summary>
    /// Sceglie la palette chiara o scura in base al tema di Windows e la cambia al volo quando l'utente cambia tema.
    /// I colori stanno in Themes/Palette.Light.xaml e Themes/Palette.Dark.xaml (stessi nomi di risorsa).
    /// </summary>
    internal static class ThemeManager
    {
        private static ResourceDictionary _current;
        private static bool _subscribed;

        public static bool IsDark { get; private set; }

        public static void Apply()
        {
            if (Application.Current == null)
            {
                return;
            }

            var dark = SystemPrefersDark();
            if (_current != null && dark == IsDark)
            {
                return;
            }

            var palette = new ResourceDictionary
            {
                Source = new Uri("/PasswordGen;component/Themes/Palette." + (dark ? "Dark" : "Light") + ".xaml", UriKind.Relative)
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_current != null)
            {
                merged.Remove(_current);
            }

            merged.Add(palette);
            _current = palette;
            IsDark = dark;

            if (!_subscribed)
            {
                _subscribed = true;
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    if (e.Category == UserPreferenceCategory.General && Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(Apply));
                    }
                };
            }
        }

        /// <summary>Per le finestre create nel codice (dialoghi): sfondo e testo seguono la palette corrente.</summary>
        public static void Style(Window window)
        {
            window.SetResourceReference(Window.BackgroundProperty, "WindowBrush");
            window.SetResourceReference(Window.ForegroundProperty, "TextBrush");
        }

        private static bool SystemPrefersDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
