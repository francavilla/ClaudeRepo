using Microsoft.Maui.ApplicationModel;
using PasswordGen.Core.Security;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Mostra la pagina di blocco sopra l'app finché l'utente non si autentica. Il blocco scatta all'avvio e dopo che l'app
/// è rimasta in secondo piano più del tempo scelto.
/// </summary>
public sealed class AppLockController
{
    private readonly AppLockState _state;
    private readonly ISecurityService _security;
    private readonly Func<Page> _root;
    private LockPage _page;
    private bool _authenticating;

    public AppLockController(AppLockState state, ISecurityService security, Func<Page> root)
    {
        _state = state;
        _security = security;
        _root = root;
    }

    public AppLockState State => _state;

    /// <summary>Da chiamare quando la finestra principale è pronta.</summary>
    public void Start()
    {
        _security.SetScreenCaptureBlocked(_state.Enabled);
        _state.Start();
        ShowIfLocked();
    }

    public void Backgrounded()
    {
        _state.Backgrounded(DateTime.UtcNow);
    }

    public void Resumed()
    {
        _state.Foregrounded(DateTime.UtcNow);
        ShowIfLocked();
    }

    /// <summary>Attiva o disattiva il blocco (l'autenticazione di conferma è già stata fatta dal chiamante).</summary>
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

    public async Task<bool> TryUnlockAsync()
    {
        if (_authenticating)
        {
            return false;
        }

        _authenticating = true;
        try
        {
            var outcome = await _security.AuthenticateAsync("PasswordGen", "Sblocca per vedere le tue password");
            if (!outcome.Success)
            {
                _page?.ShowMessage("Non sbloccata: " + outcome.Reason);
                return false;
            }

            _state.Unlocked();
            await CloseAsync();
            return true;
        }
        finally
        {
            _authenticating = false;
        }
    }

    private void ShowIfLocked()
    {
        if (_state.IsLocked)
        {
            _ = ShowAsync();
        }
    }

    private async Task ShowAsync()
    {
        if (_page != null)
        {
            return;
        }

        // Durante l'avvio la finestra può non essere ancora pronta a ricevere una pagina modale: si riprova qualche volta.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var root = _root();
                if (root != null && root.Window != null)
                {
                    var page = new LockPage(this);
                    _page = page;
                    await MainThread.InvokeOnMainThreadAsync(() => root.Navigation.PushModalAsync(page, false));
                    _ = TryUnlockAsync();
                    return;
                }
            }
            catch (Exception)
            {
                _page = null;
            }

            await Task.Delay(300);
        }
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
