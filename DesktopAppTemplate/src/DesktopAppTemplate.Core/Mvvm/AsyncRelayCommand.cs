using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DesktopAppTemplate.Core.Mvvm
{
    /// <summary>
    /// Comando asincrono senza parametro. Non è rientrante (resta disabilitato mentre è in esecuzione).
    /// Le eccezioni vengono passate a <see cref="ErrorHandler"/>; senza gestore risalgono al chiamante.
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public Action<Exception> ErrorHandler { get; set; }

        public bool CanExecute(object parameter) => !_isRunning && (_canExecute == null || _canExecute());

        public async void Execute(object parameter)
        {
            await ExecuteAsync();
        }

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
                return;

            _isRunning = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            catch (Exception ex) when (ErrorHandler != null)
            {
                ErrorHandler(ex);
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Comando asincrono con parametro tipizzato (vedi <see cref="AsyncRelayCommand"/>).</summary>
    public sealed class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public Action<Exception> ErrorHandler { get; set; }

        public bool CanExecute(object parameter) => !_isRunning && (_canExecute == null || _canExecute(Convert(parameter)));

        public async void Execute(object parameter)
        {
            await ExecuteAsync(Convert(parameter));
        }

        public async Task ExecuteAsync(T parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isRunning = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute(parameter);
            }
            catch (Exception ex) when (ErrorHandler != null)
            {
                ErrorHandler(ex);
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        private static T Convert(object parameter) => parameter is T ? (T)parameter : default(T);
    }
}
