using System.Collections.Generic;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Context;
using DesktopAppTemplate.Core.Mvvm;

namespace DesktopAppTemplate.Features.About
{
    /// <summary>
    /// Pagina "Informazioni": versione, runtime, interfaccia attiva e configurazione in uso con la sua origine.
    /// Esempio di consumo del contesto applicativo.
    /// </summary>
    public sealed class AboutViewModel : PageViewModel
    {
        public AboutViewModel(IAppContext context)
            : base("Informazioni", "", 90)
        {
            AppName = context.App.Name;
            Version = "v" + context.App.Version;
            Description = context.App.Description;
            Runtime = context.App.RuntimeDescription;
            UiName = context.Environment.UiName;
            UserName = context.Environment.UserName;
            MachineName = context.Environment.MachineName;
            Configuration = context.Configuration.Entries;
        }

        public string AppName { get; }
        public string Version { get; }
        public string Description { get; }
        public string Runtime { get; }

        /// <summary>Interfaccia utente in uso (WPF o Windows Forms).</summary>
        public string UiName { get; }

        public string UserName { get; }
        public string MachineName { get; }

        /// <summary>Valori di configurazione in uso, con l'origine di ciascuno.</summary>
        public IReadOnlyList<ConfigurationEntry> Configuration { get; }
    }
}
