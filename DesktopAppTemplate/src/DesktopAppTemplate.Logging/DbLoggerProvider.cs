using System;
using System.Collections.Generic;
using System.Globalization;
using DesktopAppTemplate.Core.Data;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    /// <summary>
    /// Scrive il log nella tabella <c>Log</c> tramite <see cref="IDbExecutor"/> (quindi con ADO.NET o con la libreria agganciata).
    /// Non inizia finché non viene chiamato <see cref="Start"/> (dopo le migrazioni dello schema): intanto i messaggi restano in coda.
    /// Se il database non risponde, i messaggi non scritti vanno in un file di ripiego: il log non si perde e non rompe l'app.
    /// </summary>
    public sealed class DbLoggerProvider : BackgroundLoggerProvider, IDeferredStart
    {
        public const string InsertSql =
            "INSERT INTO Log (LoggedAt, Level, Category, Message, Exception, UserName, MachineName) " +
            "VALUES (@LoggedAt, @Level, @Category, @Message, @Exception, @UserName, @MachineName)";

        public const string DeleteOldSql = "DELETE FROM Log WHERE LoggedAt < @Cutoff";

        private readonly IDbExecutor _executor;
        private readonly LogFileWriter _fallback;
        private readonly int _retentionDays;
        private bool _cleanedUp;

        public DbLoggerProvider(IDbExecutor executor, LogLevel minimumLevel, LogFileWriter fallback, int retentionDays)
            : base(minimumLevel, startWriting: false)
        {
            _executor = executor;
            _fallback = fallback;
            _retentionDays = retentionDays;
        }

        /// <summary>Da chiamare quando lo schema del database è pronto (o si è rinunciato): inizia la scrittura.</summary>
        public void Start() => ReleaseWriter();

        protected override void Write(IReadOnlyList<LogEntry> entries)
        {
            DeleteOldRowsOnce();

            List<LogEntry> failed = null;
            foreach (var entry in entries)
            {
                try
                {
                    // Siamo su un thread in background senza contesto di sincronizzazione: l'attesa sincrona è sicura.
                    _executor.ExecuteAsync(InsertSql, ToParameters(entry)).GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    if (failed == null)
                        failed = new List<LogEntry>();
                    failed.Add(entry);
                }
            }

            if (failed != null)
                _fallback.Write(failed);
        }

        private void DeleteOldRowsOnce()
        {
            if (_cleanedUp)
                return;

            _cleanedUp = true;
            try
            {
                var cutoff = DateTime.Now.AddDays(-_retentionDays).ToString("o", CultureInfo.InvariantCulture);
                _executor.ExecuteAsync(DeleteOldSql, new Dictionary<string, object> { { "Cutoff", cutoff } }).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // La pulizia è facoltativa: si riprova al prossimo avvio.
            }
        }

        private static Dictionary<string, object> ToParameters(LogEntry entry)
        {
            return new Dictionary<string, object>
            {
                { "LoggedAt", entry.Timestamp.ToString("o", CultureInfo.InvariantCulture) },
                { "Level", entry.Level.ToString() },
                { "Category", entry.Category },
                { "Message", entry.Message },
                { "Exception", entry.Exception },
                { "UserName", Environment.UserName },
                { "MachineName", Environment.MachineName }
            };
        }
    }
}
