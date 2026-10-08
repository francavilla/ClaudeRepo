namespace DesktopAppTemplate.Core.Context
{
    /// <summary>Ambiente in cui l'applicazione sta girando.</summary>
    public interface IAppEnvironment
    {
        /// <summary>Interfaccia utente attiva (es. "WPF", "Windows Forms").</summary>
        string UiName { get; }

        string UserName { get; }
        string MachineName { get; }
    }
}
