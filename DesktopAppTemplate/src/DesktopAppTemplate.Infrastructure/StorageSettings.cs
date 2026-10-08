using System;
using System.Collections.Generic;
using System.IO;
using DesktopAppTemplate.Core.Configuration;
using DesktopAppTemplate.Core.Data;

namespace DesktopAppTemplate.Infrastructure
{
    /// <summary>Database in cui si salvano i dati.</summary>
    public enum StorageKind
    {
        Sqlite,
        SqlServer
    }

    /// <summary>Come si esegue l'SQL: ADO.NET direttamente oppure tramite la libreria DAL.</summary>
    public enum DataAccessKind
    {
        /// <summary>ADO.NET (<c>AdoNetExecutor</c>).</summary>
        Ado,
        /// <summary>Libreria DAL (connessione singleton, transazioni esplicite): adattatore <c>Data.Dal</c>.</summary>
        Dal
    }

    /// <summary>Impostazioni di archiviazione: dove e come si salvano i dati.</summary>
    public sealed class StorageSettings
    {
        public const string DataFolderKey = "DataFolder";
        public const string StorageKey = "Storage";
        public const string DataAccessKey = "DataAccess";
        public const string ConnectionStringKey = "ConnectionString";
        public const string NoMigrateKey = "NoMigrate";

        /// <summary>Connessione predefinita a SQL Server: istanza predefinita del computer locale (<c>.</c>), database ClaudeDB, autenticazione di Windows.</summary>
        public const string DefaultSqlServerConnectionString = "Server=.;Database=ClaudeDB;Integrated Security=True";

        public static readonly OptionDefinition DataFolderOption = new OptionDefinition(
            DataFolderKey,
            "Cartella del database SQLite predefinito; sono ammesse variabili come %USERPROFILE%. Se vuota: %LocalAppData%\\<nome app>.",
            "cartella");

        public static readonly OptionDefinition StorageOption = new OptionDefinition(
            StorageKey, "Database in cui salvare i dati.", "sqlserver|sqlite", "sqlserver",
            validate: OptionValidators.OneOf("sqlite", "sqlserver"));

        public static readonly OptionDefinition DataAccessOption = new OptionDefinition(
            DataAccessKey, "Come si accede al database: ADO.NET diretto o libreria DAL.", "ado|dal", "ado",
            validate: OptionValidators.OneOf("ado", "dal"));

        public static readonly OptionDefinition ConnectionStringOption = new OptionDefinition(
            ConnectionStringKey,
            "Stringa di connessione al database. Se vuota: con sqlserver " + DefaultSqlServerConnectionString +
            " (il database si crea se manca); con sqlite un file nella cartella dei dati.",
            "stringa");

        public static readonly OptionDefinition NoMigrateOption = new OptionDefinition(
            NoMigrateKey,
            "Non crea né aggiorna le tabelle all'avvio: lo schema deve già esistere (creato con gli script di Data/Scripts).",
            isFlag: true);

        public StorageSettings(string dataFolder = null, StorageKind storage = StorageKind.SqlServer,
            DataAccessKind dataAccess = DataAccessKind.Ado, string connectionString = null, bool noMigrate = false)
        {
            DataFolder = dataFolder ?? string.Empty;
            Storage = storage;
            DataAccess = dataAccess;
            ConnectionString = connectionString ?? string.Empty;
            NoMigrate = noMigrate;
        }

        /// <summary>Cartella configurata (può essere vuota: vale il percorso predefinito).</summary>
        public string DataFolder { get; }

        public StorageKind Storage { get; }

        public DataAccessKind DataAccess { get; }

        /// <summary>Stringa di connessione configurata (può essere vuota).</summary>
        public string ConnectionString { get; }

        /// <summary>Se true l'app non crea né aggiorna le tabelle all'avvio: lo schema deve già esistere.</summary>
        public bool NoMigrate { get; }

        public DatabaseProvider Provider => Storage == StorageKind.SqlServer ? DatabaseProvider.SqlServer : DatabaseProvider.Sqlite;

        /// <summary>Opzioni da dichiarare nel catalogo dell'applicazione.</summary>
        public static IEnumerable<OptionDefinition> Options =>
            new[] { DataFolderOption, StorageOption, DataAccessOption, ConnectionStringOption, NoMigrateOption };

        public static StorageSettings From(IAppConfiguration configuration)
        {
            return new StorageSettings(
                configuration.GetString(DataFolderKey),
                ParseStorage(configuration.GetString(StorageKey)),
                ParseDataAccess(configuration.GetString(DataAccessKey)),
                configuration.GetString(ConnectionStringKey),
                configuration.GetBool(NoMigrateKey));
        }

        /// <summary>Cartella effettiva: quella configurata (con le variabili d'ambiente espanse) oppure <c>%LocalAppData%\nome app</c>, che non richiede permessi di amministratore.</summary>
        public string ResolveFolder(string appName)
        {
            if (!string.IsNullOrWhiteSpace(DataFolder))
                return Environment.ExpandEnvironmentVariables(DataFolder.Trim());

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
        }

        /// <summary>
        /// Stringa di connessione effettiva: quella configurata (variabili espanse) oppure quella predefinita del database scelto:
        /// <see cref="DefaultSqlServerConnectionString"/> per SQL Server, un file <c>nome app.db</c> nella cartella dei dati per SQLite.
        /// </summary>
        public string ResolveConnectionString(string appName)
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
                return Environment.ExpandEnvironmentVariables(ConnectionString.Trim());

            return Storage == StorageKind.Sqlite
                ? "Data Source=" + Path.Combine(ResolveFolder(appName), appName + ".db")
                : DefaultSqlServerConnectionString;
        }

        private static StorageKind ParseStorage(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "sqlite": return StorageKind.Sqlite;
                default: return StorageKind.SqlServer;
            }
        }

        private static DataAccessKind ParseDataAccess(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "dal": return DataAccessKind.Dal;
                default: return DataAccessKind.Ado;
            }
        }
    }
}
