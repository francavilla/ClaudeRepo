using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DesktopAppTemplate.Logging
{
    /// <summary>Scrive il log su file (uno al giorno, con eliminazione dei più vecchi).</summary>
    public sealed class FileLoggerProvider : BackgroundLoggerProvider
    {
        private readonly LogFileWriter _writer;

        public FileLoggerProvider(string folder, LogLevel minimumLevel, int retentionDays, string filePrefix = "app")
            : base(minimumLevel, startWriting: true)
        {
            _writer = new LogFileWriter(folder, filePrefix, retentionDays);
        }

        protected override void Write(IReadOnlyList<LogEntry> entries) => _writer.Write(entries);
    }
}
