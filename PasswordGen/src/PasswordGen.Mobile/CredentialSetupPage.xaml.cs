using PasswordGen.Core.Security;

namespace PasswordGen.Mobile;

/// <summary>Scelta di un nuovo PIN o di una nuova password dell'app (due volte, per evitare errori di battitura).</summary>
public partial class CredentialSetupPage : ContentPage
{
    private readonly CredentialKind _kind;
    private readonly TaskCompletionSource<string> _result = new TaskCompletionSource<string>();

    public CredentialSetupPage(CredentialKind kind)
    {
        InitializeComponent();
        _kind = kind;

        if (kind == CredentialKind.Pin)
        {
            TitleLabel.Text = "Scegli un PIN";
            HintLabel.Text = "Da " + LockCredential.PinMinLength + " a " + LockCredential.PinMaxLength + " cifre. Più è lungo, più è sicuro: meglio 6 cifre o più.";
            FirstEntry.Placeholder = "Nuovo PIN";
            SecondEntry.Placeholder = "Ripeti il PIN";
            FirstEntry.Keyboard = Keyboard.Numeric;
            SecondEntry.Keyboard = Keyboard.Numeric;
            FirstEntry.MaxLength = LockCredential.PinMaxLength;
            SecondEntry.MaxLength = LockCredential.PinMaxLength;
        }
        else
        {
            TitleLabel.Text = "Scegli una password";
            HintLabel.Text = "Da " + LockCredential.PasswordMinLength + " a " + LockCredential.PasswordMaxLength + " caratteri. Non serve che sia la stessa di nessun altro posto.";
            FirstEntry.Placeholder = "Nuova password";
            SecondEntry.Placeholder = "Ripeti la password";
            FirstEntry.MaxLength = LockCredential.PasswordMaxLength;
            SecondEntry.MaxLength = LockCredential.PasswordMaxLength;
        }
    }

    /// <summary>Il testo scelto, oppure null se l'utente ha annullato.</summary>
    public Task<string> Result => _result.Task;

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var first = FirstEntry.Text ?? string.Empty;
        var problem = LockCredential.Validate(_kind, first);
        if (problem == null && first != (SecondEntry.Text ?? string.Empty))
        {
            problem = _kind == CredentialKind.Pin ? "I due PIN non coincidono." : "Le due password non coincidono.";
        }

        if (problem != null)
        {
            ErrorLabel.Text = problem;
            ErrorLabel.IsVisible = true;
            return;
        }

        await FinishAsync(first);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await FinishAsync(null);
    }

    // Il tasto indietro vale come «Annulla».
    protected override bool OnBackButtonPressed()
    {
        _ = FinishAsync(null);
        return true;
    }

    private async Task FinishAsync(string secret)
    {
        FirstEntry.Text = string.Empty;
        SecondEntry.Text = string.Empty;
        try
        {
            await Navigation.PopModalAsync(true);
        }
        finally
        {
            _result.TrySetResult(secret);
        }
    }
}
