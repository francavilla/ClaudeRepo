using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using BuildExe.ViewModels;

namespace BuildExe
{
    /// <summary>
    /// Code-behind limitato a comportamenti puramente visivi: auto-scroll del log e drag &amp; drop del file.
    /// </summary>
    public partial class MainWindow : Window
    {
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

        private void OnLogChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            }
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
