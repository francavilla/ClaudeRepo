using DesktopAppTemplate.Core.Abstractions;
using DesktopAppTemplate.Core.Mvvm;

namespace DesktopAppTemplate.Features.About
{
    /// <summary>Pagina "Informazioni": mostra versione, runtime e interfaccia attiva.</summary>
    public sealed class AboutViewModel : PageViewModel
    {
        public AboutViewModel(IAppInfo appInfo, IUiDescriptor ui)
            : base("Informazioni", "", 90)
        {
            AppName = appInfo.Name;
            Version = "v" + appInfo.Version;
            Description = appInfo.Description;
            Runtime = appInfo.RuntimeDescription;
            UiName = ui.Name;
        }

        public string AppName { get; }
        public string Version { get; }
        public string Description { get; }
        public string Runtime { get; }

        /// <summary>Interfaccia utente in uso (WPF o Windows Forms).</summary>
        public string UiName { get; }
    }
}
