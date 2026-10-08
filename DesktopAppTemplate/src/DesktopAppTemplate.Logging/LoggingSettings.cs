using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DesktopAppTemplate.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    /// <summary>Dove scrivere il log.</summary>
    [Flags]
    public enum LogTargets
    {
        None = 0,
        File = 1,
        Database = 2
    }

    /// <summary>Impostazioni del logging: destinazioni, livello minimo, cartella dei file e conservazione.</summary>
    public sealed class LoggingSettings
    {
        public const string TargetsKey = "LogTargets";
        public const string LevelKey = "LogLevel";
        public const string FolderKey = "LogFolder";
        public const string RetentionKey = "LogRetentionDays";

        public static readonly OptionDefinition TargetsOption = new OptionDefinition(
            TargetsKey, "Dove scrivere il log: file, db, entrambi (file,db) oppure nessuno (none).", "file|db|file,db|none", "file",
            validate: value => TryParseTargets(value, out LogTargets ignored) ? null : "valori ammessi: file, db, file,db, none");

        public static readonly OptionDefinition LevelOption = new OptionDefinition(
            LevelKey, "Livello minimo registrato.", "trace|debug|information|warning|error|critical|none", "information",
            validate: value => TryParseLevel(value, out LogLevel ignored) ? null : "valori ammessi: trace, debug, information, warning, error, critical, none");

        public static readonly OptionDefinition FolderOption = new OptionDefinition(
            FolderKey, "Cartella dei file di log. Se vuota: la sottocartella logs della cartella dei dati.", "cartella");

        public static readonly OptionDefinition RetentionOption = new OptionDefinition(
            RetentionKey, "Giorni di conservazione dei log (file e righe nel database).", "giorni", "30",
            validate: value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int days) && days >= 1 ? null : "serve un numero intero di giorni, almeno 1");

        public LoggingSettings(LogTargets targets = LogTargets.File, LogLevel level = LogLevel.Information, string folder = null, int retentionDays = 30)
        {
            Targets = targets;
            Level = level;
            Folder = folder ?? string.Empty;
            RetentionDays = retentionDays;
        }

        public LogTargets Targets { get; }
        public LogLevel Level { get; }

        /// <summary>Cartella configurata (può essere vuota: vale quella predefinita).</summary>
        public string Folder { get; }

        public int RetentionDays { get; }

        /// <summary>Opzioni da dichiarare nel catalogo dell'applicazione.</summary>
        public static IEnumerable<OptionDefinition> Options => new[] { TargetsOption, LevelOption, FolderOption, RetentionOption };

        public static LoggingSettings From(IAppConfiguration configuration)
        {
            LogTargets targets;
            LogLevel level;
            int days;

            return new LoggingSettings(
                TryParseTargets(configuration.GetString(TargetsKey), out targets) ? targets : LogTargets.File,
                TryParseLevel(configuration.GetString(LevelKey), out level) ? level : LogLevel.Information,
                configuration.GetString(FolderKey),
                int.TryParse(configuration.GetString(RetentionKey), NumberStyles.None, CultureInfo.InvariantCulture, out days) && days >= 1 ? days : 30);
        }

        /// <summary>Cartella effettiva dei file di log: quella configurata (variabili d'ambiente espanse) oppure <paramref name="defaultFolder"/>.</summary>
        public string ResolveFolder(string defaultFolder)
        {
            return string.IsNullOrWhiteSpace(Folder) ? defaultFolder : Environment.ExpandEnvironmentVariables(Folder.Trim());
        }

        /// <summary>Destinazioni: "file", "db"/"database", "none"; separate da virgola, punto e virgola o spazio. Vuoto = file.</summary>
        public static bool TryParseTargets(string value, out LogTargets targets)
        {
            targets = LogTargets.File;
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var tokens = value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLowerInvariant()).ToList();
            var result = LogTargets.None;
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case "file":
                        result |= LogTargets.File;
                        break;
                    case "db":
                    case "database":
                        result |= LogTargets.Database;
                        break;
                    case "none":
                        if (tokens.Count > 1)
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            targets = result;
            return true;
        }

        public static bool TryParseLevel(string value, out LogLevel level)
        {
            level = LogLevel.Information;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "information":
                case "info":
                    level = LogLevel.Information;
                    return true;
                case "trace":
                    level = LogLevel.Trace;
                    return true;
                case "debug":
                    level = LogLevel.Debug;
                    return true;
                case "warning":
                case "warn":
                    level = LogLevel.Warning;
                    return true;
                case "error":
                    level = LogLevel.Error;
                    return true;
                case "critical":
                    level = LogLevel.Critical;
                    return true;
                case "none":
                    level = LogLevel.None;
                    return true;
                default:
                    return false;
            }
        }
    }
}
