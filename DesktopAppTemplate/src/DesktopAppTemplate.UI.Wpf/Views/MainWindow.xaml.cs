using System.Windows;
using DesktopAppTemplate.Features.Shell;

namespace DesktopAppTemplate.UI.Wpf.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Gli errori dei comandi sono già gestiti dai view model.
            await ((MainViewModel)DataContext).InitializeAsync();
        }
    }
}
