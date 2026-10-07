using System.Collections.Generic;

namespace ExeBuilder.Services
{
    /// <summary>Interazioni con l'utente e con la shell, astratte per tenere il ViewModel testabile.</summary>
    public interface IDialogService
    {
        string PickProjectOrSolution(string initialPath);

        string PickFolder(string initialPath);

        void ShowError(string message);

        void OpenFolder(string path);

        void CopyToClipboard(string text);
    }
}
