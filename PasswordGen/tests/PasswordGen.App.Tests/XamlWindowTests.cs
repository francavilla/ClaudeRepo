using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PasswordGen.Converters;
using Xunit;

namespace PasswordGen.App.Tests
{
    /// <summary>
    /// Apre la vera finestra principale con un ViewModel reale, ne visita ogni scheda e sezione e raccoglie gli errori di binding e di risorse
    /// che WPF segnala solo a runtime (proprietà inesistente, associazione in due direzioni su sola lettura, risorsa mancante, ...).
    /// La compilazione non li vede: prima di questo test emergevano solo aprendo l'app.
    /// </summary>
    public class XamlWindowTests
    {
        private const string PackPrefix = "pack://application:,,,/PasswordGen;component/Themes/";

        [Fact]
        public void FinestraPrincipale_SiApreESiVisitaSenzaErroriDiBindingOdiRisorse()
        {
            var problems = new List<string>();
            RunSta(() => VisitWindow(problems));

            Assert.True(problems.Count == 0, "Errori trovati aprendo la finestra:" + Environment.NewLine + string.Join(Environment.NewLine + "---" + Environment.NewLine, problems));
        }

        private static void VisitWindow(List<string> problems)
        {
            var listener = new CollectingListener();
            PresentationTraceSources.Refresh();
            foreach (var source in new[] { PresentationTraceSources.DataBindingSource, PresentationTraceSources.ResourceDictionarySource })
            {
                source.Listeners.Add(listener);
                source.Switch.Level = SourceLevels.Warning;
            }

            // Come in App.xaml / ThemeManager: il tema e i convertitori stanno nelle risorse dell'applicazione.
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.DispatcherUnhandledException += (s, e) =>
            {
                problems.Add("Eccezione non gestita: " + e.Exception);
                e.Handled = true;
            };
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(PackPrefix + "Theme.xaml") });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(PackPrefix + "Palette.Light.xaml") });
            app.Resources.Add("BoolToVisibility", new BooleanToVisibilityConverter());
            app.Resources.Add("InverseBoolToVisibility", new InverseBooleanToVisibilityConverter());

            // Le due palette (chiara e scura) devono definire le stesse risorse: se ne manca una, il tema scuro si rompe.
            var light = new ResourceDictionary { Source = new Uri(PackPrefix + "Palette.Light.xaml") };
            var dark = new ResourceDictionary { Source = new Uri(PackPrefix + "Palette.Dark.xaml") };
            var missingInDark = light.Keys.Cast<object>().Except(dark.Keys.Cast<object>()).ToList();
            var missingInLight = dark.Keys.Cast<object>().Except(light.Keys.Cast<object>()).ToList();
            if (missingInDark.Count > 0 || missingInLight.Count > 0)
            {
                problems.Add("Palette diverse. Mancano nella scura: " + string.Join(", ", missingInDark) + "; mancano nella chiara: " + string.Join(", ", missingInLight));
            }

            using (var host = new MainViewModelTests())
            {
                var harness = host.CreateViewModel("finestra");
                var vm = harness.ViewModel;

                var window = new MainWindow { DataContext = vm, ShowInTaskbar = false };
                window.Show();
                Pump();

                // Cambio password in corso (proposte da scegliere e titolo), poi registrato (cronologia con titolo e senza).
                vm.GenerateCommand.Execute(null);
                vm.MarkChangedCommand.Execute(null);
                Pump();
                VisitAll(window);

                vm.ChoiceIndex = 1;
                vm.ChoiceLabel = "Portale HR";
                vm.ConfirmChangeCommand.Execute(null);
                vm.MarkChangedCommand.Execute(null);
                vm.ChoiceIndex = 2;
                vm.ConfirmChangeCommand.Execute(null);
                Pump();

                // Ogni tipo di password mostra parti diverse della finestra.
                vm.IsPassphraseMode = true;
                Pump();
                VisitAll(window);
                vm.IsSyllablesMode = true;
                Pump();
                VisitAll(window);
                vm.IsRandomMode = true;
                Pump();
                VisitAll(window);

                window.Close();
                Pump();
                app.Shutdown();
            }

            problems.AddRange(listener.Messages);
        }

        /// <summary>Seleziona a turno ogni scheda (anche annidata) ed espande ogni sezione: i contenuti non visibili non vengono associati finché non servono.</summary>
        private static void VisitAll(DependencyObject root)
        {
            VisitTabs(root, new HashSet<TabControl>());
            Expand(root);
        }

        private static void VisitTabs(DependencyObject root, HashSet<TabControl> visited)
        {
            foreach (var tabs in Descendants<TabControl>(root).Where(t => !visited.Contains(t)).ToList())
            {
                visited.Add(tabs);
                var original = tabs.SelectedIndex;
                for (var i = 0; i < tabs.Items.Count; i++)
                {
                    tabs.SelectedIndex = i;
                    Pump();
                    Expand(tabs);
                    VisitTabs(tabs, visited);
                }

                tabs.SelectedIndex = original < 0 ? 0 : original;
                Pump();
            }
        }

        private static void Expand(DependencyObject root)
        {
            foreach (var expander in Descendants<Expander>(root).ToList())
            {
                expander.IsExpanded = true;
            }

            Pump();
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var typed = child as T;
                if (typed != null)
                {
                    yield return typed;
                }

                foreach (var nested in Descendants<T>(child))
                {
                    yield return nested;
                }
            }
        }

        /// <summary>Lascia lavorare WPF (layout, binding, caricamento) finché non resta nulla di urgente.</summary>
        private static void Pump()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>Raccoglie i messaggi (completi) che WPF scrive nel registro di diagnostica.</summary>
        private sealed class CollectingListener : TraceListener
        {
            private readonly object _gate = new object();
            private readonly StringBuilder _line = new StringBuilder();
            private readonly List<string> _messages = new List<string>();

            public IList<string> Messages
            {
                get { lock (_gate) { return _messages.ToList(); } }
            }

            public override void Write(string message)
            {
                lock (_gate) { _line.Append(message); }
            }

            public override void WriteLine(string message)
            {
                lock (_gate)
                {
                    _line.Append(message);
                    _messages.Add(_line.ToString());
                    _line.Clear();
                }
            }
        }
    }
}
