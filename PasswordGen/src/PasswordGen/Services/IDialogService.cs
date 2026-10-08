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
    }
}
