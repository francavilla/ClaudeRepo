using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using PasswordGen.Core.Generation;

namespace PasswordGen.Core.History
{
    /// <summary>
    /// Storico degli ultimi cambi password, dal più recente. Conserva al massimo <see cref="MaxEntries"/> voci:
    /// le più vecchie vengono eliminate.
    /// </summary>
    [DataContract]
    public sealed class PasswordHistory
    {
        public const int MaxEntries = 12;

        [DataMember(Name = "Entries")]
        private List<HistoryEntry> _entries = new List<HistoryEntry>();

        [DataMember]
        private int _nextNumber = 1;

        public IReadOnlyList<HistoryEntry> Entries
        {
            get { return _entries; }
        }

        /// <summary>Il deserializzatore non esegue inizializzatori e costruttore: i valori predefiniti vanno impostati qui.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _entries = new List<HistoryEntry>();
            _nextNumber = 1;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _entries = _entries ?? new List<HistoryEntry>();
            _nextNumber = Math.Max(1, _nextNumber);
            Trim();
        }

        /// <summary>Registra un cambio. Con <paramref name="password"/> nulla o vuota si registra solo la data.</summary>
        public HistoryEntry Add(string password, GenerationMode mode, DateTime date)
        {
            var entry = new HistoryEntry
            {
                Number = _nextNumber++,
                DateText = date.ToString(HistoryEntry.DateFormat, CultureInfo.InvariantCulture),
                Mode = mode,
                Password = password ?? string.Empty
            };

            _entries.Insert(0, entry);
            Trim();
            return entry;
        }

        /// <summary>
        /// Unisce voci provenienti da un altro dispositivo: non cancella mai nulla di locale. Due voci sono la stessa se hanno
        /// la stessa data e la stessa password; una voce con la sola data è assorbita da una voce con password dello stesso giorno.
        /// Restituisce quante voci nuove sono rimaste nello storico (dopo il limite di <see cref="MaxEntries"/>).
        /// </summary>
        public int Merge(IEnumerable<HistoryEntry> incoming)
        {
            var added = new List<HistoryEntry>();
            foreach (var other in incoming ?? Enumerable.Empty<HistoryEntry>())
            {
                if (other == null || !other.Date.HasValue)
                {
                    continue;
                }

                var password = other.Password ?? string.Empty;
                var date = other.DateText;
                if (_entries.Any(e => e.DateText == date && string.Equals(e.Password ?? string.Empty, password, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (password.Length == 0)
                {
                    if (_entries.Any(e => e.DateText == date))
                    {
                        continue;   // la data è già coperta da un'altra voce
                    }
                }
                else
                {
                    _entries.RemoveAll(e => e.DateText == date && !e.HasPassword);
                }

                var entry = new HistoryEntry
                {
                    Number = _nextNumber++,
                    DateText = date,
                    Mode = Enum.IsDefined(typeof(GenerationMode), other.Mode) ? other.Mode : GenerationMode.Passphrase,
                    Password = password
                };
                _entries.Add(entry);
                added.Add(entry);
            }

            // Ordinamento stabile: dal giorno più recente; a parità di giorno restano prime le voci già presenti.
            _entries = _entries.OrderByDescending(e => e.Date ?? DateTime.MinValue).ToList();
            Trim();
            return added.Count(a => _entries.Contains(a));
        }

        public bool Remove(int number)
        {
            return _entries.RemoveAll(e => e.Number == number) > 0;
        }

        public void Clear()
        {
            _entries.Clear();
            _nextNumber = 1;
        }

        /// <summary>Le password conservate, dalla più recente: servono a evitare di riproporre varianti.</summary>
        public IReadOnlyList<string> Passwords()
        {
            return _entries.Where(e => e.HasPassword).Select(e => e.Password).ToList();
        }

        private void Trim()
        {
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
            }
        }
    }
}
