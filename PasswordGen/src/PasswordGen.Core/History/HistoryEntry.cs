using System;
using System.Globalization;
using System.Runtime.Serialization;
using PasswordGen.Core.Generation;

namespace PasswordGen.Core.History
{
    /// <summary>Un cambio password registrato nello storico.</summary>
    [DataContract]
    public sealed class HistoryEntry
    {
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>Numero progressivo, mai riutilizzato (anche dopo una cancellazione).</summary>
        [DataMember] public int Number { get; set; }

        /// <summary>Data del cambio, in formato yyyy-MM-dd.</summary>
        [DataMember] public string DateText { get; set; }

        [DataMember] public GenerationMode Mode { get; set; }

        /// <summary>La password usata; vuota se è stata registrata solo la data del cambio.</summary>
        [DataMember] public string Password { get; set; }

        public bool HasPassword
        {
            get { return !string.IsNullOrEmpty(Password); }
        }

        public DateTime? Date
        {
            get
            {
                DateTime parsed;
                return DateTime.TryParseExact(DateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                    ? parsed
                    : (DateTime?)null;
            }
        }
    }
}
