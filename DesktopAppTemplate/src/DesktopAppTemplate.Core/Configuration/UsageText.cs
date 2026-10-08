using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Genera il testo di <c>--help</c> dalle opzioni dichiarate: non va mai tenuto aggiornato a mano.</summary>
    public static class UsageText
    {
        public static string Build(string appName, string version, IEnumerable<OptionDefinition> options)
        {
            var rows = new List<KeyValuePair<string, string>>();
            foreach (var option in options)
            {
                var left = option.IsFlag
                    ? option.CommandLineName
                    : option.CommandLineName + " <" + (option.ValueHint ?? "valore") + ">";

                var notes = new List<string>();
                if (!string.IsNullOrEmpty(option.DefaultValue) && !option.IsFlag)
                    notes.Add("predefinito: " + option.DefaultValue);
                notes.Add("App.config: " + option.Key);

                rows.Add(new KeyValuePair<string, string>(left, option.Description + " [" + string.Join("; ", notes) + "]"));
            }

            rows.Add(new KeyValuePair<string, string>("-v, --version", "Mostra la versione ed esce."));
            rows.Add(new KeyValuePair<string, string>("-h, --help, /?", "Mostra questo aiuto ed esce."));

            var width = rows.Max(r => r.Key.Length) + 2;
            var lines = new List<string>
            {
                appName + " " + version,
                string.Empty,
                "Utilizzo: " + appName + ".exe [opzioni]",
                string.Empty,
                "Opzioni (la riga di comando prevale su App.config, che prevale sui valori predefiniti):"
            };
            lines.AddRange(rows.Select(r => "  " + r.Key.PadRight(width) + r.Value));
            lines.Add(string.Empty);
            lines.Add("Le opzioni a valore si scrivono \"--nome valore\" oppure \"--nome=valore\"; i flag \"--nome\" oppure \"--nome=false\".");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
