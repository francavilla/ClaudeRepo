using PasswordGen.Core.Security;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile;

/// <summary>Schermata che copre l'app finché l'utente non si autentica (o, in modalità conferma, finché non conferma la sua identità).</summary>
public partial class LockPage : ContentPage
{
    private readonly AppLockController _controller;
    private readonly bool _confirmMode;
    private bool _timerRunning;

    public LockPage(AppLockController controller, bool confirmMode)
    {
        InitializeComponent();
        _controller = controller;
        _confirmMode = confirmMode;

        if (confirmMode)
        {
            TitleLabel.Text = "Conferma la tua identità";
            CancelButton.IsVisible = true;
        }

        HintLabel.Text = "Scegli come sbloccare.";

        if (controller.HasCredential)
        {
            var isPin = controller.CredentialKind == CredentialKind.Pin;
            SecretEntry.IsVisible = true;
            SecretButton.IsVisible = true;
            SecretEntry.Placeholder = isPin ? "PIN" : "Password";
            if (isPin)
            {
                SecretEntry.Keyboard = Keyboard.Numeric;
                SecretEntry.MaxLength = LockCredential.PinMaxLength;
            }
        }

        BiometricButton.IsVisible = controller.PhoneUnlockAvailable;
        PhonePinButton.IsVisible = controller.PhoneUnlockAvailable;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateCountdown();
        if (SecretEntry.IsVisible)
        {
            SecretEntry.Focus();
        }
    }

    /// <summary>Mostra il motivo per cui lo sblocco non è riuscito.</summary>
    public void ShowMessage(string message)
    {
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrEmpty(message);
    }

    private async void OnSecretClicked(object sender, EventArgs e)
    {
        var text = SecretEntry.Text ?? string.Empty;
        SecretEntry.Text = string.Empty;
        if (text.Length == 0)
        {
            return;
        }

        var attempt = await _controller.TryUnlockWithSecretAsync(text);
        if (!attempt.Success)
        {
            ShowMessage(attempt.Message);
            UpdateCountdown();
        }
    }

    private async void OnBiometricClicked(object sender, EventArgs e)
    {
        await _controller.TryUnlockAsync();
    }

    private async void OnPhonePinClicked(object sender, EventArgs e)
    {
        await _controller.TryUnlockAsync(useDeviceCredential: true);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await _controller.CancelConfirmationAsync();
    }

    /// <summary>Dopo troppi errori il campo del PIN resta disattivato finché non passa l'attesa; il conteggio scende ogni secondo.</summary>
    private void UpdateCountdown()
    {
        if (!_controller.HasCredential)
        {
            return;
        }

        var remaining = _controller.RetryAfter;
        var waiting = remaining > TimeSpan.Zero;
        SecretEntry.IsEnabled = !waiting;
        SecretButton.IsEnabled = !waiting;
        if (waiting)
        {
            ShowMessage("Troppi tentativi sbagliati: riprova tra " + AppLockController.Seconds(remaining) + ".");
            if (!_timerRunning)
            {
                _timerRunning = true;
                Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
                {
                    UpdateCountdown();
                    _timerRunning = _controller.RetryAfter > TimeSpan.Zero;
                    if (!_timerRunning)
                    {
                        ShowMessage(string.Empty);
                    }

                    return _timerRunning;
                });
            }
        }
    }

    // Il tasto indietro non deve chiudere il blocco (in modalità conferma vale «Annulla»).
    protected override bool OnBackButtonPressed()
    {
        if (_confirmMode)
        {
            _ = _controller.CancelConfirmationAsync();
        }

        return true;
    }
}
