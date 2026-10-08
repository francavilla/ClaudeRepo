using System.Collections.Generic;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Configurazione applicativa risolta (valori predefiniti + App.config + riga di comando). Di sola lettura.</summary>
    public interface IAppConfiguration
    {
        /// <summary>Valore della chiave (senza distinzione tra maiuscole e minuscole), oppure null se non impostata.</summary>
        string GetString(string key);

        /// <summary>Origine del valore della chiave, oppure null se non impostata.</summary>
        string GetSource(string key);

        /// <summary>Tutti i valori, ordinati per chiave.</summary>
        IReadOnlyList<ConfigurationEntry> Entries { get; }
    }

    public static class AppConfigurationExtensions
    {
        /// <summary>Legge un valore booleano ("true"/"false"); se manca o non è valido restituisce <paramref name="defaultValue"/>.</summary>
        public static bool GetBool(this IAppConfiguration configuration, string key, bool defaultValue = false)
        {
            bool value;
            return bool.TryParse(configuration.GetString(key), out value) ? value : defaultValue;
        }
    }
}
