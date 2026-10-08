namespace DesktopAppTemplate.Core.Abstractions
{
    /// <summary>Avvia l'interfaccia utente e resta in esecuzione fino alla chiusura. Restituisce il codice di uscita.</summary>
    public interface IUiShell
    {
        int Run();
    }
}
