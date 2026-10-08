using System;
using System.Text;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>
    /// Dichiara un parametro dell'applicazione una volta sola. Da questa riga derivano:
    /// la lettura da App.config (chiave <see cref="Key"/>), la lettura da riga di comando
    /// (<see cref="CommandLineName"/>, ricavato dalla chiave: "DataFolder" → "--data-folder"),
    /// la validazione e il testo di <c>--help</c>.
    /// </summary>
    public sealed class OptionDefinition
    {
        private readonly Func<string, string> _validate;

        /// <param name="key">Chiave in App.config e nella configurazione (es. "DataFolder").</param>
        /// <param name="description">Descrizione mostrata da <c>--help</c>.</param>
        /// <param name="valueHint">Esempio dei valori per <c>--help</c> (es. "wpf|winforms"); non serve per i flag.</param>
        /// <param name="defaultValue">Valore se non è indicato altrove (null = nessun valore).</param>
        /// <param name="isFlag">Opzione senza valore: <c>--read-only</c> equivale a <c>--read-only=true</c>.</param>
        /// <param name="validate">Restituisce il motivo dell'errore, o null se il valore è valido.</param>
        public OptionDefinition(string key, string description, string valueHint = null, string defaultValue = null,
            bool isFlag = false, Func<string, string> validate = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("La chiave è obbligatoria.", nameof(key));

            Key = key.Trim();
            Description = description ?? string.Empty;
            ValueHint = valueHint;
            IsFlag = isFlag;
            DefaultValue = isFlag && defaultValue == null ? "false" : defaultValue;
            _validate = isFlag && validate == null ? OptionValidators.Boolean : validate;
            CommandLineName = "--" + ToKebabCase(Key);
        }

        public string Key { get; }
        public string Description { get; }
        public string ValueHint { get; }
        public string DefaultValue { get; }
        public bool IsFlag { get; }
        public string CommandLineName { get; }

        /// <summary>Restituisce il motivo per cui il valore non è valido, oppure null.</summary>
        public string Validate(string value) => _validate?.Invoke(value);

        internal static string ToKebabCase(string key)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < key.Length; i++)
            {
                var c = key[i];
                if (char.IsUpper(c) && i > 0)
                    builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }
    }
}
