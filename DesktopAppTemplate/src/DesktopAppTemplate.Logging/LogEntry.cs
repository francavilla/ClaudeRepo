using System;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    /// <summary>Una riga di log, già formattata, pronta per essere scritta su file o database.</summary>
    public sealed class LogEntry
    {
        public LogEntry(DateTime timestamp, LogLevel level, string category, string message, string exception)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string Category { get; }
        public string Message { get; }

        /// <summary>Testo completo dell'eccezione (tipo, messaggio, stack), oppure null.</summary>
        public string Exception { get; }

        /// <summary>Riga di testo per i file: <c>2026-10-08 09:30:15.123 [INF] Categoria - Messaggio</c>, con l'eccezione sotto.</summary>
        public string ToLine()
        {
            var line = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + ShortLevel(Level) + "] " + Category + " - " + Message;
            return Exception == null ? line : line + Environment.NewLine + Exception;
        }

        private static string ShortLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRC";
                case LogLevel.Debug: return "DBG";
                case LogLevel.Information: return "INF";
                case LogLevel.Warning: return "WRN";
                case LogLevel.Error: return "ERR";
                case LogLevel.Critical: return "CRT";
                default: return "???";
            }
        }
    }
}
