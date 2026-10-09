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
        public const int MaxLabelLength = 60;

        /// <summary>Numero progressivo, mai riutilizzato (anche dopo una cancellazione).</summary>
        [DataMember] public int Number { get; set; }

        /// <summary>Data del cambio, in formato yyyy-MM-dd.</summary>
        [DataMember] public string DateText { get; set; }

        [DataMember] public GenerationMode Mode { get; set; }

        /// <summary>La password usata; vuota se è stata registrata solo la data del cambio.</summary>
        [DataMember] public string Password { get; set; }

        /// <summary>Quando la voce è stata registrata (UTC, yyyy-MM-ddTHH:mm:ssZ); vuota per le voci create prima dell'azzeramento condiviso.</summary>
        [DataMember] public string AddedUtcText { get; set; }

        /// <summary>Titolo facoltativo che ricorda dove è usata la password (per esempio «Portale HR»).</summary>
        [DataMember] public string Label { get; set; }

        /// <summary>Quando il titolo è stato scritto o modificato l'ultima volta (UTC): decide quale titolo vince nella sincronizzazione.</summary>
        [DataMember] public string LabelUtcText { get; set; }

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
