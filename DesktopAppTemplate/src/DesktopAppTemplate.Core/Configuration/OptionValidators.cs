using System;
using System.Linq;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Validatori pronti per <see cref="OptionDefinition"/>: restituiscono il motivo dell'errore, o null se il valore è valido.</summary>
    public static class OptionValidators
    {
        public static string Boolean(string value)
        {
            bool ignored;
            return bool.TryParse(value, out ignored) ? null : "valori ammessi: true, false";
        }

        public static Func<string, string> OneOf(params string[] allowed)
        {
            return value => allowed.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                ? null
                : "valori ammessi: " + string.Join(", ", allowed);
        }
    }
}
