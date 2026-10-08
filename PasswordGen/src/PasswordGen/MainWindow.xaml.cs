using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
            Loaded += OnLoaded;
            Activated += (s, e) => Lock?.Activated();
            Deactivated += (s, e) => Lock?.Deactivated();
        }

        private PasswordGen.Services.AppLockController Lock
        {
            get
            {
                var vm = DataContext as MainViewModel;
                return vm == null ? null : vm.Lock;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null)
            {
                return;
            }

            // Quando l'app si blocca il contenuto sotto la schermata di blocco non deve essere raggiungibile (nemmeno con Tab).
            vm.Lock.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.Lock.IsLocked))
                {
                    MainContent.IsEnabled = !vm.Lock.IsLocked;
                    if (vm.Lock.IsLocked)
                    {
                        PreviousBox.Password = string.Empty;
                        vm.ClearSensitive();
                        UnlockBox.Clear();
                        if (vm.Lock.HasCredential)
                        {
                            Dispatcher.BeginInvoke(new Action(() => UnlockBox.Focus()));
                        }
                    }
                }
            };
            MainContent.IsEnabled = !vm.Lock.IsLocked;

            await vm.Lock.StartAsync();
            await vm.AutoSyncAsync();
        }

        private async void OnUnlockWithSecret(object sender, RoutedEventArgs e)
        {
            await TryUnlockWithSecretAsync();
        }

        private async void OnUnlockBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await TryUnlockWithSecretAsync();
            }
        }

        private async Task TryUnlockWithSecretAsync()
        {
            var lockController = Lock;
            if (lockController == null)
            {
                return;
            }

            var secret = UnlockBox.Password;
            UnlockBox.Clear();
            if (secret.Length == 0)
            {
                return;
            }

            await lockController.TryUnlockWithSecretAsync(secret);
            if (lockController.IsLocked)
            {
                UnlockBox.Focus();
            }
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
