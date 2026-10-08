using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Risultato della lettura della riga di comando.</summary>
    public sealed class CommandLineResult
    {
        /// <summary>Valori indicati, per chiave di configurazione (es. "Ui" → "wpf").</summary>
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool HelpRequested { get; internal set; }
        public bool VersionRequested { get; internal set; }

        /// <summary>Argomenti sconosciuti o incompleti.</summary>
        public List<string> Errors { get; } = new List<string>();
    }

    /// <summary>
    /// Legge la riga di comando secondo le opzioni dichiarate: <c>--nome valore</c>, <c>--nome=valore</c>, flag <c>--nome</c>.
    /// Gestisce da sé <c>--help</c> (<c>-h</c>, <c>-?</c>, <c>/?</c>) e <c>--version</c> (<c>-v</c>).
    /// </summary>
    public static class CommandLineParser
    {
        private static readonly string[] HelpFlags = { "-h", "--help", "-?", "/?", "/h" };
        private static readonly string[] VersionFlags = { "-v", "--version", "/v" };

        public static CommandLineResult Parse(IEnumerable<string> args, IEnumerable<OptionDefinition> options)
        {
            var byName = options.ToDictionary(o => o.CommandLineName, StringComparer.OrdinalIgnoreCase);
            var list = (args ?? Enumerable.Empty<string>()).Select(a => (a ?? string.Empty).Trim()).Where(a => a.Length > 0).ToList();
            var result = new CommandLineResult();

            for (var i = 0; i < list.Count; i++)
            {
                var arg = list[i];

                if (HelpFlags.Contains(arg, StringComparer.OrdinalIgnoreCase))
                {
                    result.HelpRequested = true;
                    continue;
                }

                if (VersionFlags.Contains(arg, StringComparer.OrdinalIgnoreCase))
                {
                    result.VersionRequested = true;
                    continue;
                }

                var name = arg;
                string inlineValue = null;
                var equals = arg.IndexOf('=');
                if (arg.StartsWith("--", StringComparison.Ordinal) && equals > 0)
                {
                    name = arg.Substring(0, equals);
                    inlineValue = arg.Substring(equals + 1);
                }

                OptionDefinition option;
                if (!name.StartsWith("--", StringComparison.Ordinal) || !byName.TryGetValue(name, out option))
                {
                    result.Errors.Add("Argomento non riconosciuto: " + arg);
                    continue;
                }

                string value;
                if (option.IsFlag)
                {
                    value = inlineValue ?? "true";
                }
                else if (inlineValue != null)
                {
                    value = inlineValue;
                }
                else if (i + 1 < list.Count && !list[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    value = list[++i];
                }
                else
                {
                    result.Errors.Add("L'opzione " + option.CommandLineName + " richiede un valore.");
                    continue;
                }

                result.Values[option.Key] = value;
            }

            return result;
        }
    }
}
