using System;
using System.Linq;

namespace DesktopAppTemplate.Core.Hosting
{
    /// <summary>Opzioni informative della riga di comando (aiuto e versione) e relativo testo.</summary>
    public static class CommandLine
    {
        private static readonly string[] HelpFlags = { "-h", "--help", "-?", "/?", "/h" };
        private static readonly string[] VersionFlags = { "-v", "--version", "/v" };

        public static bool IsHelpRequested(string[] args) => Contains(args, HelpFlags);

        public static bool IsVersionRequested(string[] args) => Contains(args, VersionFlags);

        /// <summary>Testo d'aiuto mostrato da <c>--help</c>.</summary>
        public static string GetUsage(string appName, string version)
        {
            var lines = new[]
            {
                appName + " " + version,
                string.Empty,
                "Utilizzo: " + appName + ".exe [opzioni]",
                string.Empty,
                "Opzioni:",
                "  --ui wpf|winforms   Sceglie l'interfaccia (anche --ui=wpf).",
                "                      Predefinita: chiave \"Ui\" di " + appName + ".exe.config, altrimenti wpf.",
                "  -v, --version       Mostra la versione ed esce.",
                "  -h, --help, /?      Mostra questo aiuto ed esce."
            };
            return string.Join(Environment.NewLine, lines);
        }

        private static bool Contains(string[] args, string[] flags)
        {
            return args != null && args.Any(a => a != null && flags.Contains(a.Trim(), StringComparer.OrdinalIgnoreCase));
        }
    }
}
