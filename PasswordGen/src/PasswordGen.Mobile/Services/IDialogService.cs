namespace PasswordGen.Mobile.Services;

/// <summary>Finestre di dialogo semplici, astratte per tenere il ViewModel privo di codice UI.</summary>
public interface IDialogService
{
    /// <summary>Chiede conferma all'utente (Sì / No).</summary>
    Task<bool> ConfirmAsync(string message, string title);

    /// <summary>Fa scegliere una voce dall'elenco; restituisce l'indice, o -1 se l'utente annulla.</summary>
    Task<int> ChooseAsync(string title, IReadOnlyList<string> options);
}
