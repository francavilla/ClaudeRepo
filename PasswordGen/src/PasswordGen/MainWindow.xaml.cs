using System;
using System.ComponentModel;
using System.Windows;
using PasswordGen.ViewModels;

namespace PasswordGen
{
    /// <summary>Code-behind limitato a comportamenti puramente visivi: dimensione della finestra e PasswordBox (non è associabile).</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FitToWorkArea();
        }

        /// <summary>Se la dimensione predefinita supera l'area utile dello schermo, la finestra viene ridotta.</summary>
        private void FitToWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            Width = Math.Max(MinWidth, Math.Min(Width, workArea.Width));
            Height = Math.Max(MinHeight, Math.Min(Height, workArea.Height));
        }

        private void OnPreviousPasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.PreviousPassword = PreviousBox.Password;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.PreviousPassword = string.Empty;
                vm.SaveSettings();
            }

            base.OnClosing(e);
        }
    }
}
