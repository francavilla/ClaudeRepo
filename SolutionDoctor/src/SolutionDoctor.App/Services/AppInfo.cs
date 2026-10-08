using System.Linq;
using System.Reflection;

namespace SolutionDoctor.App.Services
{
    /// <summary>
    /// Versione dell'applicazione letta dall'assembly. Si imposta in un solo punto:
    /// &lt;Version&gt; in Directory.Build.props (l'SDK la copia in AssemblyVersion, FileVersion e InformationalVersion).
    /// </summary>
    public static class AppInfo
    {
        private static readonly Assembly Assembly = typeof(AppInfo).Assembly;

        /// <summary>Versione leggibile, es. "1.1.0".</summary>
        public static string Version
        {
            get
            {
                var informational = Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .OfType<AssemblyInformationalVersionAttribute>()
                    .Select(a => a.InformationalVersion)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(informational))
                {
                    // L'SDK può aggiungere "+<hash del commit>": nell'interfaccia non serve.
                    var plus = informational.IndexOf('+');
                    return plus > 0 ? informational.Substring(0, plus) : informational;
                }

                return Assembly.GetName().Version.ToString(3);
            }
        }
    }
}
