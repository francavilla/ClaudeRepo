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
        public const int MaxEntries = 20;

        [DataMember(Name = "Entries")]
        private List<HistoryEntry> _entries = new List<HistoryEntry>();

        [DataMember]
        private int _nextNumber = 1;

        /// <summary>
        /// Quando lo storico è stato azzerato l'ultima volta (UTC). Viaggia con la sincronizzazione: le voci registrate prima di questo
        /// momento vengono eliminate anche sugli altri dispositivi e non tornano più.
        /// </summary>
        [DataMember(Name = "ResetUtc")]
        private string _resetUtcText;

        public string ResetUtcText
        {
            get { return _resetUtcText; }
        }

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
            _resetUtcText = null;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _entries = _entries ?? new List<HistoryEntry>();
            _nextNumber = Math.Max(1, _nextNumber);
            Trim();
        }

        /// <summary>Registra un cambio. Con <paramref name="password"/> nulla o vuota si registra solo la data.</summary>
        public HistoryEntry Add(string password, GenerationMode mode, DateTime date, DateTime? addedUtc = null)
        {
            var entry = new HistoryEntry
            {
                Number = _nextNumber++,
                DateText = date.ToString(HistoryEntry.DateFormat, CultureInfo.InvariantCulture),
                Mode = mode,
                Password = password ?? string.Empty,
                AddedUtcText = addedUtc.HasValue ? ExchangeTime(addedUtc.Value) : null
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

                if (IsBeforeReset(other.AddedUtcText))
                {
                    continue;   // registrata prima dell'ultimo azzeramento: non deve tornare
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
                    Password = password,
                    AddedUtcText = other.AddedUtcText
                };
                _entries.Add(entry);
                added.Add(entry);
            }

            // Ordinamento stabile: dal giorno più recente; a parità di giorno restano prime le voci già presenti.
            _entries = _entries.OrderByDescending(e => e.Date ?? DateTime.MinValue).ToList();
            Trim();
            return added.Count(a => _entries.Contains(a));
        }

        /// <summary>Copia indipendente dello storico (voci e numerazione).</summary>
        public PasswordHistory Clone()
        {
            var copy = new PasswordHistory { _nextNumber = _nextNumber, _resetUtcText = _resetUtcText };
            foreach (var e in _entries)
            {
                copy._entries.Add(new HistoryEntry { Number = e.Number, DateText = e.DateText, Mode = e.Mode, Password = e.Password, AddedUtcText = e.AddedUtcText });
            }

            return copy;
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

        /// <summary>
        /// Azzera lo storico e ne ricorda il momento: la sincronizzazione lo propaga agli altri dispositivi.
        /// </summary>
        public void Reset(DateTime nowUtc)
        {
            Clear();
            var stamp = ExchangeTime(nowUtc);
            var current = ParseExchangeTime(_resetUtcText);
            if (!current.HasValue || ParseExchangeTime(stamp) > current)
            {
                _resetUtcText = stamp;
            }
        }

        /// <summary>
        /// Adotta un azzeramento arrivato da un altro dispositivo, se è più recente del nostro: le voci registrate prima
        /// (o senza data di registrazione) vengono eliminate. Restituisce true se qualcosa è cambiato.
        /// </summary>
        public bool ApplyReset(string incomingResetUtcText)
        {
            var incoming = ParseExchangeTime(incomingResetUtcText);
            if (!incoming.HasValue)
            {
                return false;
            }

            var current = ParseExchangeTime(_resetUtcText);
            if (current.HasValue && incoming.Value <= current.Value)
            {
                return false;
            }

            _resetUtcText = incomingResetUtcText;
            _entries.RemoveAll(e => IsBeforeReset(e.AddedUtcText));
            return true;
        }

        private bool IsBeforeReset(string addedUtcText)
        {
            var reset = ParseExchangeTime(_resetUtcText);
            if (!reset.HasValue)
            {
                return false;
            }

            var added = ParseExchangeTime(addedUtcText);
            return !added.HasValue || added.Value < reset.Value;
        }

        private const string TimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        private static string ExchangeTime(DateTime utc)
        {
            return utc.ToUniversalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime? ParseExchangeTime(string text)
        {
            DateTime parsed;
            return DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed)
                ? parsed
                : (DateTime?)null;
        }

        /// <summary>
        /// La voce più recente con questa stessa password (confronto identico: maiuscole e minuscole contano), oppure null.
        /// Serve alla regola aziendale «la nuova password non può essere una delle ultime <see cref="MaxEntries"/>».
        /// </summary>
        public HistoryEntry Find(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return null;
            }

            return _entries.FirstOrDefault(e => e.HasPassword && string.Equals(e.Password, password, StringComparison.Ordinal));
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
