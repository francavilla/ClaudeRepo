using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Risultato di <see cref="ConfigurationBuilder.Build"/>.</summary>
    public sealed class ConfigurationBuildResult
    {
        public ConfigurationBuildResult(IAppConfiguration configuration, IReadOnlyList<string> errors)
        {
            Configuration = configuration;
            Errors = errors;
        }

        public IAppConfiguration Configuration { get; }

        /// <summary>Valori non validi (vuoto se è tutto a posto).</summary>
        public IReadOnlyList<string> Errors { get; }
    }

    /// <summary>
    /// Costruisce la configurazione sovrapponendo le sorgenti, dalla più debole alla più forte:
    /// valori predefiniti delle opzioni, poi le sorgenti aggiunte in ordine (l'ultima vince).
    /// Le chiavi non dichiarate come opzione vengono ignorate.
    /// </summary>
    public sealed class ConfigurationBuilder
    {
        public const string DefaultSourceName = "predefinito";
        public const string CommandLineSourceName = "riga di comando";

        private readonly Dictionary<string, OptionDefinition> _options;
        private readonly List<KeyValuePair<string, IReadOnlyList<KeyValuePair<string, string>>>> _sources =
            new List<KeyValuePair<string, IReadOnlyList<KeyValuePair<string, string>>>>();

        public ConfigurationBuilder(IEnumerable<OptionDefinition> options)
        {
            _options = options.ToDictionary(o => o.Key, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Aggiunge una sorgente: prevale su quelle aggiunte prima.</summary>
        public ConfigurationBuilder AddSource(string name, IEnumerable<KeyValuePair<string, string>> values)
        {
            _sources.Add(new KeyValuePair<string, IReadOnlyList<KeyValuePair<string, string>>>(name, values.ToList()));
            return this;
        }

        /// <summary>Aggiunge come sorgente le impostazioni di un <see cref="NameValueCollection"/> (es. <c>ConfigurationManager.AppSettings</c>).</summary>
        public ConfigurationBuilder AddNameValueSource(string name, NameValueCollection values)
        {
            var pairs = values.AllKeys
                .Where(k => k != null)
                .Select(k => new KeyValuePair<string, string>(k, values[k]))
                .ToList();
            return AddSource(name, pairs);
        }

        public ConfigurationBuildResult Build()
        {
            var entries = new Dictionary<string, ConfigurationEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in _options.Values.Where(o => o.DefaultValue != null))
                entries[option.Key] = new ConfigurationEntry(option.Key, option.DefaultValue, DefaultSourceName);

            foreach (var source in _sources)
            {
                foreach (var pair in source.Value)
                {
                    OptionDefinition option;
                    if (pair.Key == null || !_options.TryGetValue(pair.Key.Trim(), out option))
                        continue;

                    entries[option.Key] = new ConfigurationEntry(option.Key, (pair.Value ?? string.Empty).Trim(), source.Key);
                }
            }

            var errors = new List<string>();
            foreach (var entry in entries.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
            {
                var option = _options[entry.Key];
                var reason = option.Validate(entry.Value);
                if (reason == null)
                    continue;

                var name = entry.Source == CommandLineSourceName ? option.CommandLineName : option.Key;
                errors.Add($"Valore non valido \"{entry.Value}\" per {name} (origine: {entry.Source}): {reason}.");
            }

            return new ConfigurationBuildResult(new AppConfiguration(entries.Values), errors);
        }
    }
}
