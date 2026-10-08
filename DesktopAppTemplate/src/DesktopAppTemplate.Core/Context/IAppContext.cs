using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Configuration;

namespace DesktopAppTemplate.Core.Context
{
    /// <summary>
    /// Contesto dell'applicazione: un unico punto da cui ogni parte (view model, handler) ottiene
    /// configurazione, informazioni, ambiente di esecuzione e stato di sessione.
    /// Chi ha bisogno di poco dovrebbe chiedere solo la parte che gli serve (es. <see cref="IAppConfiguration"/>).
    /// </summary>
    public interface IAppContext
    {
        /// <summary>Configurazione risolta all'avvio (non cambia).</summary>
        IAppConfiguration Configuration { get; }

        /// <summary>Nome, versione e runtime dell'applicazione (non cambia).</summary>
        IAppInfo App { get; }

        /// <summary>Ambiente di esecuzione: interfaccia attiva, utente, computer (non cambia).</summary>
        IAppEnvironment Environment { get; }

        /// <summary>Stato di lavoro corrente, condiviso tra le pagine (modificabile).</summary>
        ISessionState Session { get; }
    }
}
