namespace DesktopAppTemplate.Core.Abstractions
{
    /// <summary>Finestre di dialogo semplici, implementate da ciascuna UI.</summary>
    public interface IDialogService
    {
        bool Confirm(string title, string message);
        void ShowError(string title, string message);
    }
}
