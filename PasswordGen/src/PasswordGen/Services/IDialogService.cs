using System.Collections.Generic;
using PasswordGen.Core.Security;

namespace PasswordGen.Services
{
    /// <summary>Finestre di dialogo semplici, astratte per tenere il ViewModel privo di codice UI.</summary>
    public interface IDialogService
    {
        /// <summary>Chiede conferma all'utente (Sì / No); No è la risposta predefinita.</summary>
        bool Confirm(string message, string title);

        /// <summary>Fa scegliere un file esistente; restituisce il percorso, o null se l'utente annulla.</summary>
        string PickFile(string title, string filter);

        /// <summary>
        /// Fa scegliere dove salvare un file. Con <paramref name="confirmOverwrite"/> falso si può scegliere anche un file già esistente
        /// senza domande (serve per il file di sincronizzazione, che si crea o si riusa). Null se l'utente annulla.
        /// </summary>
        string PickSaveFile(string title, string filter, string fileName, bool confirmOverwrite);

        /// <summary>Chiede una frase segreta (due volte se <paramref name="confirm"/>); null se l'utente annulla.</summary>
        string AskPassphrase(string title, string message, bool confirm);

        /// <summary>Chiede un testo breve (per esempio il titolo di una password), con un valore iniziale; null se l'utente annulla, stringa vuota se lo svuota.</summary>
        string AskText(string title, string message, string initial);

        /// <summary>Fa scegliere una voce da un elenco; restituisce l'indice, o -1 se l'utente annulla.</summary>
        int Choose(string title, IReadOnlyList<string> options);

        /// <summary>Chiede due volte un nuovo PIN o una nuova password; restituisce il testo scelto, o null se l'utente annulla.</summary>
        string AskNewSecret(CredentialKind kind);

        /// <summary>Chiede il PIN o la password dell'app già impostati; null se l'utente annulla.</summary>
        string AskSecret(string title, string message);
    }
}
