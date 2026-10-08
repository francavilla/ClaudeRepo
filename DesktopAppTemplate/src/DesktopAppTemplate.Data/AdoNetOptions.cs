using System;
using System.Data.Common;

namespace DesktopAppTemplate.Data
{
    /// <summary>Opzioni modificabili dell'executor ADO.NET predefinito.</summary>
    public sealed class AdoNetOptions
    {
        /// <summary>Timeout dei comandi in secondi (0 = nessun timeout).</summary>
        public int CommandTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// PUNTO DI AGGANCIO: chiamato su ogni comando appena creato, prima dell'esecuzione. Serve ad esempio per
        /// tracciare le query, impostare proprietà specifiche del database o applicare regole della libreria esistente.
        /// </summary>
        public Action<DbCommand> ConfigureCommand { get; set; }
    }
}
