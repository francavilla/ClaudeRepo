using System;
using System.Globalization;
using System.Runtime.Serialization;
using PasswordGen.Core.Generation;
using PasswordGen.Core.Policy;

namespace PasswordGen.Core.Settings
{
    /// <summary>
    /// Preferenze dell'utente. Non contengono mai password: solo opzioni di generazione,
    /// regole della policy e data dell'ultimo cambio password.
    /// </summary>
    [DataContract]
    public sealed class AppSettings
    {
        public const string DateFormat = "yyyy-MM-dd";
        public const int MinSuggestions = 1;
        public const int MaxSuggestions = 20;
        public const int DefaultSuggestions = 6;

        public AppSettings()
        {
            ApplyDefaults();
        }

        [DataMember] public GenerationMode Mode { get; set; }
        [DataMember] public int WordCount { get; set; }
        [DataMember] public int SyllableCount { get; set; }
        [DataMember] public int RandomLength { get; set; }

        /// <summary>Quante proposte mostrare per volta.</summary>
        [DataMember] public int SuggestionCount { get; set; }

        [DataMember] public int MinLength { get; set; }
        [DataMember] public bool RequireUpper { get; set; }
        [DataMember] public bool RequireLower { get; set; }
        [DataMember] public bool RequireDigit { get; set; }
        [DataMember] public bool RequireSpecial { get; set; }
        [DataMember] public bool AvoidAmbiguous { get; set; }

        /// <summary>Conserva le password scelte in uno storico cifrato (history.dat).</summary>
        [DataMember] public bool HistoryEnabled { get; set; }

        /// <summary>Sorgente delle parole per le passphrase.</summary>
        [DataMember] public WordSourceMode WordSource { get; set; }

        /// <summary>Percorso del file di parole dell'utente (nullo se non caricato).</summary>
        [DataMember] public string CustomWordsPath { get; set; }

        [DataMember] public bool ReminderEnabled { get; set; }
        [DataMember] public int ValidityDays { get; set; }
        [DataMember] public int WarnDays { get; set; }

        /// <summary>Data dell'ultimo cambio password, in formato yyyy-MM-dd (vuota se mai registrata).</summary>
        [DataMember] public string LastChangeDateText { get; set; }

        public DateTime? LastChangeDate
        {
            get
            {
                DateTime parsed;
                return DateTime.TryParseExact(LastChangeDateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                    ? parsed
                    : (DateTime?)null;
            }
            set
            {
                LastChangeDateText = value.HasValue ? value.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : null;
            }
        }

        /// <summary>Il deserializzatore non esegue il costruttore: i valori predefiniti vanno impostati qui.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            var policy = new PasswordPolicy();
            var options = new GenerationOptions();

            Mode = options.Mode;
            WordCount = options.WordCount;
            SyllableCount = options.SyllableCount;
            RandomLength = options.RandomLength;
            SuggestionCount = DefaultSuggestions;

            MinLength = policy.MinLength;
            RequireUpper = policy.RequireUpper;
            RequireLower = policy.RequireLower;
            RequireDigit = policy.RequireDigit;
            RequireSpecial = policy.RequireSpecial;
            AvoidAmbiguous = policy.AvoidAmbiguous;

            HistoryEnabled = true;
            WordSource = WordSourceMode.Builtin;
            CustomWordsPath = null;
            ReminderEnabled = true;
            ValidityDays = 30;
            WarnDays = 5;
            LastChangeDateText = null;
        }

        /// <summary>Riporta i valori letti dal file entro limiti sensati.</summary>
        public AppSettings Normalized()
        {
            MinLength = Clamp(MinLength, PasswordPolicy.MinAllowedLength, PasswordPolicy.MaxAllowedLength);
            WordCount = Clamp(WordCount, GenerationOptions.MinWords, GenerationOptions.MaxWords);
            SyllableCount = Clamp(SyllableCount, GenerationOptions.MinSyllables, GenerationOptions.MaxSyllables);
            RandomLength = Clamp(RandomLength, PasswordPolicy.MinAllowedLength, GenerationOptions.MaxRandomLength);
            SuggestionCount = Clamp(SuggestionCount, MinSuggestions, MaxSuggestions);
            ValidityDays = Clamp(ValidityDays, 7, 365);
            WarnDays = Clamp(WarnDays, 0, 30);
            if (!Enum.IsDefined(typeof(WordSourceMode), WordSource))
            {
                WordSource = WordSourceMode.Builtin;
            }

            if (!Enum.IsDefined(typeof(GenerationMode), Mode))
            {
                Mode = GenerationMode.Passphrase;
            }

            return this;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
