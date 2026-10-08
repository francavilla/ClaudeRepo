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

    public async Task<bool> TryUnlockAsync()
    {
        if (_authenticating)
        {
            return false;
        }

        _authenticating = true;
        try
        {
            if (!await _security.AuthenticateAsync("PasswordGen", "Sblocca per vedere le tue password"))
            {
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
        try
        {
            if (_page != null)
            {
                return;
            }

            var root = _root();
            if (root == null)
            {
                return;
            }

            _page = new LockPage(this);
            await root.Navigation.PushModalAsync(_page, false);
            _ = TryUnlockAsync();
        }
        catch (Exception)
        {
            _page = null;
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
