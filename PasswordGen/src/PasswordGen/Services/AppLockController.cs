using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using PasswordGen.Core.Security;
using PasswordGen.Mvvm;

namespace PasswordGen.Services
{
    /// <summary>Esito del tentativo di sblocco con il PIN o la password dell'app.</summary>
    public sealed class SecretAttempt
    {
        public SecretAttempt(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; private set; }

        public string Message { get; private set; }
    }

    /// <summary>
    /// Blocco dell'app: all'avvio e dopo che la finestra è rimasta in secondo piano per il tempo scelto una schermata copre tutto finché
    /// l'utente non si autentica con Windows Hello (PIN, impronta o volto) oppure con il PIN o la password dell'app (con attesa crescente
    /// dopo gli errori). Il PIN o la password dell'app permettono il blocco anche su PC senza Windows Hello.
    /// </summary>
    public sealed class AppLockController : ObservableObject
    {
        private readonly AppLockState _state;
        private readonly IWindowsHello _hello;
        private readonly Func<IntPtr> _windowHandle;
        private readonly LockCredentialManager _credentials;
        private DispatcherTimer _timer;
        private bool _authenticating;
        private bool _checking;
        private bool _helloAvailable;
        private string _message = string.Empty;
        private string _waitText = string.Empty;

        /// <param name="credentials">Gestore del PIN/password dell'app; nullo se non si vuole offrire questa possibilità.</param>
        public AppLockController(AppLockState state, IWindowsHello hello, Func<IntPtr> windowHandle, LockCredentialManager credentials = null)
        {
            _state = state;
            _hello = hello;
            _windowHandle = windowHandle;
            _credentials = credentials;
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

        /// <summary>Conto alla rovescia dell'attesa dopo troppi tentativi sbagliati (vuoto se si può provare).</summary>
        public string WaitText
        {
            get { return _waitText; }
            private set { SetProperty(ref _waitText, value); }
        }

        public ICommand UnlockCommand { get; private set; }

        // ---------------------------------------------------------------- PIN o password dell'app

        /// <summary>True se si può impostare un PIN o una password dell'app.</summary>
        public bool CredentialsSupported
        {
            get { return _credentials != null; }
        }

        public bool HasCredential
        {
            get { return _credentials != null && _credentials.HasCredential; }
        }

        public CredentialKind? CredentialKind
        {
            get { return _credentials == null ? (CredentialKind?)null : _credentials.Kind; }
        }

        /// <summary>True se Windows Hello è configurato su questo computer.</summary>
        public bool HelloAvailable
        {
            get { return _helloAvailable; }
            private set { SetProperty(ref _helloAvailable, value); }
        }

        /// <summary>Sulla schermata di blocco: la casella del PIN o della password dell'app.</summary>
        public bool ShowSecretEntry
        {
            get { return HasCredential; }
        }

        public string SecretPrompt
        {
            get
            {
                if (!HasCredential)
                {
                    return string.Empty;
                }

                return CredentialKind == Core.Security.CredentialKind.Pin ? "Inserisci il PIN dell'app" : "Inserisci la password dell'app";
            }
        }

        /// <summary>Testo guida della schermata di blocco, in base a come ci si può sbloccare.</summary>
        public string Prompt
        {
            get
            {
                if (HasCredential && HelloAvailable)
                {
                    return "Usa Windows Hello oppure il PIN o la password dell'app per continuare.";
                }

                return HasCredential ? "Usa il PIN o la password dell'app per continuare." : "Usa Windows Hello (PIN, impronta o volto) per continuare.";
            }
        }

        public Task SetCredentialAsync(CredentialKind kind, string secret)
        {
            // L'hash richiede un po' di calcolo (di proposito): fuori dal thread dell'interfaccia.
            return Task.Run(() => _credentials.Set(kind, secret));
        }

        public void ClearCredential()
        {
            if (_credentials != null)
            {
                _credentials.Clear();
            }

            RefreshCredentialProperties();
        }

        public void RefreshCredentialProperties()
        {
            OnPropertyChanged(nameof(HasCredential));
            OnPropertyChanged(nameof(CredentialKind));
            OnPropertyChanged(nameof(ShowSecretEntry));
            OnPropertyChanged(nameof(SecretPrompt));
            OnPropertyChanged(nameof(Prompt));
        }

        /// <summary>Verifica il PIN o la password dell'app senza sbloccare (serve per confermare un'azione).</summary>
        public async Task<SecretAttempt> VerifySecretAsync(string secret)
        {
            if (_credentials == null || !_credentials.HasCredential || _checking)
            {
                return new SecretAttempt(false, string.Empty);
            }

            _checking = true;
            try
            {
                var result = await Task.Run(() => _credentials.Check(secret));
                StartCountdownIfNeeded();
                switch (result.Outcome)
                {
                    case CredentialCheck.Correct:
                        return new SecretAttempt(true, string.Empty);
                    case CredentialCheck.LockedOut:
                        return new SecretAttempt(false, "Troppi tentativi sbagliati: riprova tra " + Seconds(result.RetryAfter) + ".");
                    default:
                        return new SecretAttempt(false, result.RetryAfter > TimeSpan.Zero
                            ? "Errato. Troppi tentativi: riprova tra " + Seconds(result.RetryAfter) + "."
                            : "Errato. Ancora " + result.FreeAttemptsLeft + (result.FreeAttemptsLeft == 1 ? " tentativo" : " tentativi") + " prima dell'attesa.");
                }
            }
            finally
            {
                _checking = false;
            }
        }

        /// <summary>Verifica il PIN o la password dell'app; se è giusto sblocca.</summary>
        public async Task<SecretAttempt> TryUnlockWithSecretAsync(string secret)
        {
            var attempt = await VerifySecretAsync(secret);
            if (attempt.Success)
            {
                Message = string.Empty;
                WaitText = string.Empty;
                _state.Unlocked();
                Refresh();
            }
            else if (!string.IsNullOrEmpty(attempt.Message))
            {
                Message = attempt.Message;
            }

            return attempt;
        }

        internal static string Seconds(TimeSpan time)
        {
            var total = (int)Math.Ceiling(time.TotalSeconds);
            return total >= 120 ? (total / 60) + " minuti" : total + " secondi";
        }

        /// <summary>Se c'è un'attesa in corso (anche dopo aver riaperto l'app) mostra il conto alla rovescia.</summary>
        public void StartCountdownIfNeeded()
        {
            if (_credentials == null || _credentials.RetryAfter() <= TimeSpan.Zero)
            {
                WaitText = string.Empty;
                return;
            }

            UpdateWaitText();
            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (s, e) => UpdateWaitText();
            }

            _timer.Start();
        }

        private void UpdateWaitText()
        {
            var remaining = _credentials == null ? TimeSpan.Zero : _credentials.RetryAfter();
            if (remaining <= TimeSpan.Zero)
            {
                WaitText = string.Empty;
                if (_timer != null)
                {
                    _timer.Stop();
                }

                return;
            }

            WaitText = "Attendi " + Seconds(remaining) + " prima di riprovare.";
        }

        // ---------------------------------------------------------------- avvio, fuoco, blocco

        /// <summary>Si verifica quando il blocco viene disattivato da solo (per esempio Windows Hello non è più configurato).</summary>
        public event EventHandler<string> DisabledAutomatically;

        /// <summary>All'avvio: se il blocco è attivo, la finestra resta coperta finché non ci si autentica.</summary>
        public async Task StartAsync()
        {
            var availability = await _hello.CheckAvailabilityAsync();
            HelloAvailable = availability.Success;
            OnPropertyChanged(nameof(Prompt));
            if (_state.Enabled && !availability.Success && !HasCredential)
            {
                // Senza Windows Hello e senza PIN o password dell'app non si può sbloccare: meglio disattivare il blocco che chiudere fuori l'utente.
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

            ScreenProtection.Apply(_windowHandle(), _state.Enabled);
            Refresh();
            StartCountdownIfNeeded();
            if (_state.IsLocked && HelloAvailable)
            {
                await TryUnlockAsync();
            }
        }

        public Task<HelloResult> CheckAvailabilityAsync()
        {
            return _hello.CheckAvailabilityAsync();
        }

        /// <summary>Ricontrolla se Windows Hello è configurato (può cambiare mentre l'app è aperta).</summary>
        public async Task RefreshAvailabilityAsync()
        {
            var availability = await _hello.CheckAvailabilityAsync();
            HelloAvailable = availability.Success;
            OnPropertyChanged(nameof(Prompt));
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
            // Mentre la finestra di Windows Hello (o una nostra finestra di dialogo) è aperta la finestra principale perde il fuoco: non conta come uscita dall'app.
            if (!_authenticating && !ExternalActivity.IsActive)
            {
                _state.Backgrounded(DateTime.UtcNow);
            }
        }

        public void Activated()
        {
            if (_authenticating || ExternalActivity.IsActive)
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
                StartCountdownIfNeeded();
                if (HelloAvailable)
                {
                    var ignored = TryUnlockAsync();
                }
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
            StartCountdownIfNeeded();
        }

        public async Task<bool> TryUnlockAsync()
        {
            if (_authenticating || !HelloAvailable)
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
                WaitText = string.Empty;
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
