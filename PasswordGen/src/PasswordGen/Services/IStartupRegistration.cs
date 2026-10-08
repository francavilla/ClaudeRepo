namespace PasswordGen.Services
{
    /// <summary>Avvio automatico dell'applicazione all'accesso a Windows, per il promemoria.</summary>
    public interface IStartupRegistration
    {
        bool IsEnabled { get; }

        void SetEnabled(bool enabled);
    }
}
