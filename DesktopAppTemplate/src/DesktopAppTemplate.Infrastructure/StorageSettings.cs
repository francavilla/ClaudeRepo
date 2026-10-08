using System;
using System.Collections.Generic;
using System.IO;
using DesktopAppTemplate.Core.Configuration;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>Impostazioni di archiviazione: dove si salvano i dati.</summary>
    public sealed class StorageSettings
    {
        public const string DataFolderKey = "DataFolder";

        public static readonly OptionDefinition DataFolderOption = new OptionDefinition(
            DataFolderKey,
            "Cartella dei dati (file tasks.json); sono ammesse variabili come %USERPROFILE%. Se vuota: %LocalAppData%\\<nome app>.",
            "cartella");

        public StorageSettings(string dataFolder)
        {
            DataFolder = dataFolder ?? string.Empty;
        }

        /// <summary>Cartella configurata (può essere vuota: vale il percorso predefinito).</summary>
        public string DataFolder { get; }

        /// <summary>Opzioni da dichiarare nel catalogo dell'applicazione.</summary>
        public static IEnumerable<OptionDefinition> Options => new[] { DataFolderOption };

        public static StorageSettings From(IAppConfiguration configuration)
        {
            return new StorageSettings(configuration.GetString(DataFolderKey));
        }

        /// <summary>Cartella effettiva: quella configurata (con le variabili d'ambiente espanse) oppure <c>%LocalAppData%\nome app</c>, che non richiede permessi di amministratore.</summary>
        public string ResolveFolder(string appName)
        {
            if (!string.IsNullOrWhiteSpace(DataFolder))
                return Environment.ExpandEnvironmentVariables(DataFolder.Trim());

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
        }
    }
}
