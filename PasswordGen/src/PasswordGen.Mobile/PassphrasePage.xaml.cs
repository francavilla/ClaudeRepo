using PasswordGen.Core.Sync;

namespace PasswordGen.Mobile;

/// <summary>Richiesta della frase segreta del file di scambio (due volte se serve sceglierla, una sola se serve ripeterla).</summary>
public partial class PassphrasePage : ContentPage
{
    private readonly bool _confirm;
    private readonly TaskCompletionSource<string> _result = new TaskCompletionSource<string>();

    public PassphrasePage(string title, string message, bool confirm)
    {
        InitializeComponent();
        _confirm = confirm;
        TitleLabel.Text = title;
        HintLabel.Text = message;
        SecondEntry.IsVisible = confirm;
        if (!confirm)
        {
            FirstEntry.Completed += OnOkClicked;
        }
    }

    /// <summary>La frase scelta, oppure null se l'utente ha annullato.</summary>
    public Task<string> Result => _result.Task;

    private async void OnOkClicked(object sender, EventArgs e)
    {
        var first = FirstEntry.Text ?? string.Empty;
        var problem = ExchangeFile.ValidatePassphrase(first);
        if (problem == null && _confirm && first != (SecondEntry.Text ?? string.Empty))
        {
            problem = "Le due frasi non coincidono.";
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

    private async Task FinishAsync(string passphrase)
    {
        FirstEntry.Text = string.Empty;
        SecondEntry.Text = string.Empty;
        try
        {
            await Navigation.PopModalAsync(true);
        }
        finally
        {
            _result.TrySetResult(passphrase);
        }
    }
}
