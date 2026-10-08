using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile;

/// <summary>Schermata che copre l'app finché l'utente non si autentica.</summary>
public partial class LockPage : ContentPage
{
    private readonly AppLockController _controller;

    public LockPage(AppLockController controller)
    {
        InitializeComponent();
        _controller = controller;
    }

    /// <summary>Mostra il motivo per cui lo sblocco non è riuscito.</summary>
    public void ShowMessage(string message)
    {
        MessageLabel.Text = message;
        MessageLabel.IsVisible = true;
    }

    private async void OnUnlockClicked(object sender, EventArgs e)
    {
        await _controller.TryUnlockAsync();
    }

    // Il tasto indietro non deve chiudere il blocco.
    protected override bool OnBackButtonPressed()
    {
        return true;
    }
}
