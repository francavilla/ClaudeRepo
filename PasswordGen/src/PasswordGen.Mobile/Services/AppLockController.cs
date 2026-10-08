using Microsoft.Maui.ApplicationModel;
using PasswordGen.Core.Security;

namespace PasswordGen.Mobile.Services;

/// <summary>Esito del tentativo di sblocco con il PIN o la password dell'app.</summary>
public sealed class SecretAttempt
{
    public SecretAttempt(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }

    public string Message { get; }
}

/// <summary>
/// Mostra la pagina di blocco sopra l'app finché l'utente non si autentica. Il blocco scatta all'avvio e dopo che l'app è rimasta
/// in secondo piano più del tempo scelto. Si sblocca con l'impronta (o il volto), con il PIN o la password dell'app, oppure con
/// il PIN del telefono.
/// </summary>
public sealed class AppLockController
{
    private readonly AppLockState _state;
    private readonly ISecurityService _security;
    private readonly LockCredentialManager _credentials;
    private readonly Func<Page> _root;
    private LockPage _page;
    private TaskCompletionSource<bool> _confirmation;
    private bool _authenticating;
    private bool _checking;

    /// <param name="credentials">Gestore del PIN/password dell'app; nullo se non c'è la chiave di cifratura (allora restano impronta e PIN del telefono).</param>
    public AppLockController(AppLockState state, ISecurityService security, LockCredentialManager credentials, Func<Page> root)
    {
        _state = state;
        _security = security;
        _credentials = credentials;
        _root = root;
    }

    public AppLockState State => _state;

    /// <summary>True se si può impostare un PIN o una password dell'app.</summary>
    public bool CredentialsSupported => _credentials != null;

    public bool HasCredential => _credentials != null && _credentials.HasCredential;

    public CredentialKind? CredentialKind => _credentials?.Kind;

    /// <summary>True se il telefono ha un blocco schermo: impronta, volto e PIN del telefono sono utilizzabili.</summary>
    public bool PhoneUnlockAvailable => _security.IsAvailable;

    /// <summary>Quanto manca prima di poter riprovare con il PIN o la password dell'app.</summary>
    public TimeSpan RetryAfter => _credentials == null ? TimeSpan.Zero : _credentials.RetryAfter();

    /// <summary>Da chiamare quando la finestra principale è pronta.</summary>
    public void Start()
    {
        _security.SetScreenCaptureBlocked(_state.Enabled);
        _state.Start();
        ShowIfLocked();
    }

    public void Backgrounded()
    {
        // Mentre è aperta la schermata del PIN (o dell'impronta) l'app esce dal primo piano: non conta come uscita.
        if (!_security.IsAuthenticating && !_authenticating)
        {
            _state.Backgrounded(DateTime.UtcNow);
        }
    }

    public void Resumed()
    {
        if (_security.IsAuthenticating || _authenticating)
        {
            return;
        }

        _state.Foregrounded(DateTime.UtcNow);
        ShowIfLocked();
    }

    /// <summary>Attiva o disattiva il blocco (la conferma è già stata fatta dal chiamante).</summary>
    public void SetEnabled(bool enabled)
    {
        _state.Enabled = enabled;
        _security.SetScreenCaptureBlocked(enabled);
    }

    /// <summary>Blocca subito l'app (pulsante «Blocca adesso»).</summary>
    public void LockNow()
    {
        _state.Lock();
        ShowIfLocked();
    }

    // ---------------------------------------------------------------- PIN o password dell'app

    public Task SetCredentialAsync(CredentialKind kind, string secret)
    {
        // L'hash richiede un po' di calcolo (di proposito): fuori dal thread dell'interfaccia.
        return Task.Run(() => _credentials.Set(kind, secret));
    }

    public void ClearCredential()
    {
        _credentials?.Clear();
    }

    /// <summary>Verifica il PIN o la password dell'app; se è giusto sblocca.</summary>
    public async Task<SecretAttempt> TryUnlockWithSecretAsync(string secret)
    {
        if (_credentials == null || _checking)
        {
            return new SecretAttempt(false, string.Empty);
        }

        _checking = true;
        try
        {
            var result = await Task.Run(() => _credentials.Check(secret));
            switch (result.Outcome)
            {
                case CredentialCheck.Correct:
                    await CompleteUnlockAsync();
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

    internal static string Seconds(TimeSpan time)
    {
        var total = (int)Math.Ceiling(time.TotalSeconds);
        return total >= 120 ? (total / 60) + " minuti" : total + " secondi";
    }

    // ---------------------------------------------------------------- impronta e PIN del telefono

    /// <param name="useDeviceCredential">True per aprire subito la schermata del PIN, sequenza o password del telefono.</param>
    public async Task<bool> TryUnlockAsync(bool useDeviceCredential = false)
    {
        if (_authenticating)
        {
            return false;
        }

        _authenticating = true;
        try
        {
            const string title = "PasswordGen";
            const string subtitle = "Sblocca per vedere le tue password";
            var outcome = useDeviceCredential
                ? await _security.AuthenticateWithDeviceCredentialAsync(title, subtitle)
                : await _security.AuthenticateAsync(title, subtitle);
            if (!outcome.Success)
            {
                _page?.ShowMessage("Non sbloccata: " + outcome.Reason);
                return false;
            }
        }
        finally
        {
            _authenticating = false;
        }

        await CompleteUnlockAsync();
        return true;
    }

    // ---------------------------------------------------------------- conferma dell'identità senza bloccare l'app

    /// <summary>
    /// Mostra la schermata di blocco per far confermare l'identità (per disattivare il blocco o cambiare il PIN) senza bloccare l'app.
    /// Restituisce true se l'utente si è autenticato, false se ha annullato.
    /// </summary>
    public async Task<bool> ConfirmIdentityAsync()
    {
        if (_page != null)
        {
            return false;
        }

        _confirmation = new TaskCompletionSource<bool>();
        if (!await PushPageAsync(confirmMode: true))
        {
            _confirmation = null;
            return false;
        }

        return await _confirmation.Task;
    }

    public async Task CancelConfirmationAsync()
    {
        var confirmation = _confirmation;
        _confirmation = null;
        await CloseAsync();
        confirmation?.TrySetResult(false);
    }

    // ---------------------------------------------------------------- pagina di blocco

    private async Task CompleteUnlockAsync()
    {
        _state.Unlocked();
        var confirmation = _confirmation;
        _confirmation = null;
        await CloseAsync();
        confirmation?.TrySetResult(true);
    }

    private void ShowIfLocked()
    {
        if (_state.IsLocked)
        {
            _ = PushPageAsync(confirmMode: false);
        }
    }

    private async Task<bool> PushPageAsync(bool confirmMode)
    {
        if (_page != null)
        {
            return false;
        }

        // Durante l'avvio la finestra può non essere ancora pronta a ricevere una pagina modale: si riprova qualche volta.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var root = _root();
                if (root != null && root.Window != null)
                {
                    var page = new LockPage(this, confirmMode);
                    _page = page;
                    await MainThread.InvokeOnMainThreadAsync(() => root.Navigation.PushModalAsync(page, false));

                    // Senza PIN o password dell'app, l'impronta parte da sola; altrimenti si aspetta che l'utente scriva il PIN.
                    if (!HasCredential && PhoneUnlockAvailable)
                    {
                        _ = TryUnlockAsync();
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                _page = null;
            }

            await Task.Delay(300);
        }

        return false;
    }

    private async Task CloseAsync()
    {
        var page = _page;
        _page = null;
        if (page != null)
        {
            await page.Navigation.PopModalAsync(false);
        }
    }
}
