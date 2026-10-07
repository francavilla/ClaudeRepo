using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ExeBuilder.ViewModels;

namespace ExeBuilder
{
    /// <summary>
    /// Code-behind limitato a comportamenti puramente visivi: auto-scroll del log e drag &amp; drop del file.
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _scrollPending;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                var vm = e.NewValue as MainViewModel;
                if (vm != null)
                {
                    vm.Log.CollectionChanged += OnLogChanged;
                }
            };
        }

        /// <summary>
        /// Lo scroll NON va fatto dentro l'evento CollectionChanged: il nostro handler può essere
        /// invocato prima che la ListBox abbia registrato l'aggiunta, e ScrollIntoView forza un
        /// layout sincrono con il generatore dei container non allineato ("ItemsControl è incoerente
        /// con l'origine elementi"). Lo rimandiamo al dispatcher e accorpiamo le richieste:
        /// un solo scroll anche quando la build produce centinaia di righe di fila.
        /// </summary>
        private void OnLogChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || _scrollPending)
            {
                return;
            }

            _scrollPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ScrollLogToEnd));
        }

        private void ScrollLogToEnd()
        {
            _scrollPending = false;
            var count = LogList.Items.Count;
            if (count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[count - 1]);
            }
        }

        private void OnProjectRowPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Non si marca l'evento come gestito: la CheckBox deve continuare a cambiare stato.
            ((ListViewItem)sender).IsSelected = true;
        }

        private void OnProjectRowGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            // Navigando con Tab sulla CheckBox di una riga, anche la riga diventa quella corrente.
            ((ListViewItem)sender).IsSelected = true;
        }

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var vm = DataContext as MainViewModel;
            if (files != null && files.Length > 0 && vm != null && !vm.IsBusy)
            {
                vm.InputPath = files.First();
            }
        }
    }
}
