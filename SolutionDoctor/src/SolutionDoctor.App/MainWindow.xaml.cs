using System;
using System.Windows;
using SolutionDoctor.Presentation.ViewModels;

namespace SolutionDoctor.App
{
    /// <summary>Code-behind limitato a un comportamento puramente visivo: il trascinamento di un file o di una cartella.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FitToWorkArea();
        }

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null && files != null && files.Length > 0 && viewModel.IsIdle)
            {
                viewModel.InputPath = files[0];
                e.Handled = true;
            }
        }

        /// <summary>La finestra non deve superare l'area utile dello schermo (schermi piccoli o scala alta).</summary>
        private void FitToWorkArea()
        {
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }
    }
}
