namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Un valore di configurazione con la sua origine (utile per capire da dove arriva).</summary>
    public sealed class ConfigurationEntry
    {
        public ConfigurationEntry(string key, string value, string source)
        {
            Key = key;
            Value = value;
            Source = source;
        }

        public string Key { get; }
        public string Value { get; }

        /// <summary>Origine del valore: "predefinito", "App.config" o "riga di comando".</summary>
        public string Source { get; }
    }
}
