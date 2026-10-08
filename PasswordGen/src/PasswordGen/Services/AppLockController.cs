using System;
using System.Threading.Tasks;
using System.Windows.Input;
using PasswordGen.Core.Security;
using PasswordGen.Mvvm;

namespace PasswordGen.Services
{
    /// <summary>
    /// Blocco dell'app con Windows Hello: all'avvio e dopo che la finestra è rimasta in secondo piano per il tempo scelto
    /// una schermata copre tutto finché l'utente non si autentica.
    /// </summary>
    public sealed class AppLockController : ObservableObject
    {
        private readonly AppLockState _state;
        private readonly IWindowsHello _hello;
        private readonly Func<IntPtr> _windowHandle;
        private bool _authenticating;
        private string _message = string.Empty;

        public AppLockController(AppLockState state, IWindowsHello hello, Func<IntPtr> windowHandle)
        {
            _state = state;
            _hello = hello;
            _windowHandle = windowHandle;
            UnlockCommand = new RelayCommand(() => { var ignored = TryUnlockAsync(); });

            // Con il blocco attivo si parte già bloccati: il contenuto non deve comparire nemmeno un istante.
            _state.Start();
        }

        public AppLockState State
        {
            get { return _state; }
        }

        public bool IsLocked
        {
            get { return _state.IsLocked; }
        }

        /// <summary>Motivo dell'ultimo sblocco non riuscito.</summary>
        public string Message
        {
            get { return _message; }
            private set { SetProperty(ref _message, value); }
        }

        public ICommand UnlockCommand { get; private set; }

        /// <summary>Si verifica quando il blocco viene disattivato da solo (per esempio Windows Hello non è più configurato).</summary>
        public event EventHandler<string> DisabledAutomatically;

        /// <summary>All'avvio: se il blocco è attivo, la finestra resta coperta finché non ci si autentica.</summary>
        public async Task StartAsync()
        {
            if (_state.Enabled)
            {
                // Se Windows Hello non è più utilizzabile non si può sbloccare: meglio disattivare il blocco che chiudere fuori l'utente.
                var availability = await _hello.CheckAvailabilityAsync();
                if (!availability.Success)
                {
                    _state.Enabled = false;
                    ScreenProtection.Apply(_windowHandle(), false);
                    Refresh();
                    var handler = DisabledAutomatically;
                    if (handler != null)
                    {
                        handler(this, "Blocco dell'app disattivato: " + availability.Reason + ".");
                    }

                    return;
                }
            }

            ScreenProtection.Apply(_windowHandle(), _state.Enabled);
            Refresh();
            if (_state.IsLocked)
            {
                await TryUnlockAsync();
            }
        }

        public Task<HelloResult> CheckAvailabilityAsync()
        {
            return _hello.CheckAvailabilityAsync();
        }

        /// <summary>Chiede Windows Hello per confermare un'azione (attivare o disattivare il blocco).</summary>
        public async Task<HelloResult> ConfirmAsync(string message)
        {
            _authenticating = true;
            try
            {
                return await _hello.AuthenticateAsync(_windowHandle(), message);
            }
            finally
            {
                _authenticating = false;
            }
        }

        public void Deactivated()
        {
            // Mentre la finestra di Windows Hello è aperta la nostra finestra perde il fuoco: non conta come uscita dall'app.
            if (!_authenticating)
            {
                _state.Backgrounded(DateTime.UtcNow);
            }
        }

        public void Activated()
        {
            if (_authenticating)
            {
                return;
            }

            var wasLocked = _state.IsLocked;
            _state.Foregrounded(DateTime.UtcNow);
            if (_state.IsLocked != wasLocked)
            {
                Refresh();
            }

            if (_state.IsLocked)
            {
                var ignored = TryUnlockAsync();
            }
        }

        /// <summary>Attiva o disattiva il blocco (l'autenticazione di conferma è già stata fatta dal chiamante).</summary>
        public void SetEnabled(bool enabled)
        {
            _state.Enabled = enabled;
            ScreenProtection.Apply(_windowHandle(), enabled);
            Refresh();
        }

        public void LockNow()
        {
            _state.Lock();
            Refresh();
        }

        public async Task<bool> TryUnlockAsync()
        {
            if (_authenticating)
            {
                return false;
            }

            _authenticating = true;
            try
            {
                var outcome = await _hello.AuthenticateAsync(_windowHandle(), "Sblocca PasswordGen per vedere le tue password");
                if (!outcome.Success)
                {
                    Message = "Non sbloccata: " + outcome.Reason + ".";
                    return false;
                }

                Message = string.Empty;
                _state.Unlocked();
                Refresh();
                return true;
            }
            finally
            {
                _authenticating = false;
            }
        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(IsLocked));
        }
    }
}
