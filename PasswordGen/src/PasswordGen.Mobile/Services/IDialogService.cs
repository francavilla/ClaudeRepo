using PasswordGen.Core.Security;

namespace PasswordGen.Mobile.Services;

/// <summary>Finestre di dialogo semplici, astratte per tenere il ViewModel privo di codice UI.</summary>
public interface IDialogService
{
    /// <summary>Chiede conferma all'utente (Sì / No).</summary>
    Task<bool> ConfirmAsync(string message, string title);

    /// <summary>Fa scegliere una voce dall'elenco; restituisce l'indice, o -1 se l'utente annulla.</summary>
    Task<int> ChooseAsync(string title, IReadOnlyList<string> options);

    /// <summary>Chiede due volte un nuovo PIN o una nuova password; restituisce il testo scelto, o null se l'utente annulla.</summary>
    Task<string> AskNewSecretAsync(CredentialKind kind);

    /// <summary>Chiede la frase segreta del file di scambio (due volte se <paramref name="confirm"/>); null se l'utente annulla.</summary>
    Task<string> AskPassphraseAsync(string title, string message, bool confirm);
}
