namespace SolutionDoctor.Presentation.Services
{
    /// <summary>Interazioni con l'utente e con la shell, astratte per tenere il ViewModel testabile.</summary>
    public interface IDialogService
    {
        /// <summary>Sceglie un file .sln o .csproj; null se l'utente annulla.</summary>
        string PickSolutionOrProject(string initialPath);

        /// <summary>Sceglie una cartella; null se l'utente annulla.</summary>
        string PickFolder(string initialPath);

        /// <summary>Sceglie dove salvare il report Markdown; null se l'utente annulla.</summary>
        string PickReportFile(string suggestedFileName);

        void ShowError(string message);

        /// <summary>Apre il file con l'applicazione associata.</summary>
        void OpenFile(string path);

        /// <summary>Apre la cartella del file in Esplora risorse, selezionandolo.</summary>
        void RevealInFolder(string path);

        void OpenFolder(string path);

        void CopyToClipboard(string text);
    }
}
