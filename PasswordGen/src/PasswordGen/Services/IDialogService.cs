namespace PasswordGen.Services
{
    /// <summary>Finestre di dialogo semplici, astratte per tenere il ViewModel privo di codice UI.</summary>
    public interface IDialogService
    {
        /// <summary>Chiede conferma all'utente (Sì / No); No è la risposta predefinita.</summary>
        bool Confirm(string message, string title);

        /// <summary>Fa scegliere un file esistente; restituisce il percorso, o null se l'utente annulla.</summary>
        string PickFile(string title, string filter);
    }
}
