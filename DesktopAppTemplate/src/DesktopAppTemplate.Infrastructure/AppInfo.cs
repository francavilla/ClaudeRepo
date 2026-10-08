using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DesktopAppTemplate.Core.Abstractions;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>Legge nome e versione dall'assembly eseguibile (la versione viene da <c>&lt;Version&gt;</c> in Directory.Build.props).</summary>
    public sealed class AppInfo : IAppInfo
    {
        public AppInfo()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                ?? FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion
                                ?? "1.0.0";

            // Le versioni recenti dell'SDK aggiungono "+<hash commit>": non serve all'utente.
            var plus = informational.IndexOf('+');
            Version = plus >= 0 ? informational.Substring(0, plus) : informational;
        }

        public string Name => "DesktopAppTemplate";

        public string Version { get; }

        public string Description => "Modello di applicazione desktop con architettura a slice, utilizzabile con interfaccia WPF o Windows Forms.";

        public string RuntimeDescription => RuntimeInformation.FrameworkDescription;
    }
}
