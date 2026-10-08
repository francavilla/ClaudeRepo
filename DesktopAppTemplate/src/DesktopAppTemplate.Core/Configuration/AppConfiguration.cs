using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopAppTemplate.Core.Configuration
{
    /// <summary>Implementazione in memoria di <see cref="IAppConfiguration"/> (creata da <see cref="ConfigurationBuilder"/>).</summary>
    public sealed class AppConfiguration : IAppConfiguration
    {
        private readonly Dictionary<string, ConfigurationEntry> _entries;

        public AppConfiguration(IEnumerable<ConfigurationEntry> entries)
        {
            _entries = entries.ToDictionary(e => e.Key, StringComparer.OrdinalIgnoreCase);
            Entries = _entries.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<ConfigurationEntry> Entries { get; }

        public string GetString(string key)
        {
            ConfigurationEntry entry;
            return key != null && _entries.TryGetValue(key, out entry) ? entry.Value : null;
        }

        public string GetSource(string key)
        {
            ConfigurationEntry entry;
            return key != null && _entries.TryGetValue(key, out entry) ? entry.Source : null;
        }
    }
}
