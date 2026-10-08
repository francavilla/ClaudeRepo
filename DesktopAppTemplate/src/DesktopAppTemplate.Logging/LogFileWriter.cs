using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DesktopAppTemplate.Logging
{
    /// <summary>
    /// Scrive i messaggi in un file al giorno (<c>prefisso-AAAAMMGG.log</c>) e, una volta al giorno, elimina i file più vecchi
    /// del periodo di conservazione. I file restano leggibili da altri programmi mentre l'app è in esecuzione.
    /// </summary>
    public sealed class LogFileWriter
    {
        private readonly string _folder;
        private readonly string _prefix;
        private readonly int _retentionDays;
        private DateTime _lastCleanup = DateTime.MinValue;

        public LogFileWriter(string folder, string prefix, int retentionDays)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("La cartella dei log è obbligatoria.", nameof(folder));

            _folder = folder;
            _prefix = prefix;
            _retentionDays = retentionDays;
        }

        public void Write(IReadOnlyList<LogEntry> entries)
        {
            if (entries.Count == 0)
                return;

            Directory.CreateDirectory(_folder);
            CleanupOnceADay();

            var encoding = new UTF8Encoding(false);
            foreach (var group in GroupByDay(entries))
            {
                using (var stream = new FileStream(PathFor(group.Key), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, encoding))
                {
                    foreach (var entry in group.Value)
                        writer.WriteLine(entry.ToLine());
                }
            }
        }

        public string PathFor(DateTime day) => Path.Combine(_folder, _prefix + "-" + day.ToString("yyyyMMdd") + ".log");

        private static Dictionary<DateTime, List<LogEntry>> GroupByDay(IReadOnlyList<LogEntry> entries)
        {
            var groups = new Dictionary<DateTime, List<LogEntry>>();
            foreach (var entry in entries)
            {
                List<LogEntry> list;
                if (!groups.TryGetValue(entry.Timestamp.Date, out list))
                    groups[entry.Timestamp.Date] = list = new List<LogEntry>();
                list.Add(entry);
            }

            return groups;
        }

        private void CleanupOnceADay()
        {
            var today = DateTime.Today;
            if (_lastCleanup == today)
                return;

            _lastCleanup = today;
            var limit = DateTime.Now.AddDays(-_retentionDays);
            foreach (var file in Directory.GetFiles(_folder, _prefix + "-*.log"))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < limit)
                        File.Delete(file);
                }
                catch (IOException)
                {
                    // File in uso o già eliminato: si riprova domani.
                }
                catch (UnauthorizedAccessException)
                {
                    // Nessun permesso: si lascia il file dov'è.
                }
            }
        }
    }
}
