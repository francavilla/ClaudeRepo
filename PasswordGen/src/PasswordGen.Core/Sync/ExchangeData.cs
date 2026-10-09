using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using PasswordGen.Core.Generation;

namespace PasswordGen.Core.Sync
{
    /// <summary>Una voce dello storico nel file di scambio (senza il numero progressivo, che è locale a ogni dispositivo).</summary>
    [DataContract]
    public sealed class ExchangeEntry
    {
        [DataMember] public string DateText { get; set; }
        [DataMember] public GenerationMode Mode { get; set; }
        [DataMember] public string Password { get; set; }

        /// <summary>Quando la voce è stata registrata (UTC); serve a decidere se è precedente a un azzeramento dello storico.</summary>
        [DataMember] public string AddedUtcText { get; set; }
    }

    /// <summary>Contenuto del file di scambio: storico, data dell'ultimo cambio e durata della password.</summary>
    [DataContract]
    public sealed class ExchangeData
    {
        public const string TimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        public ExchangeData()
        {
            Format = 1;
            Entries = new List<ExchangeEntry>();
        }

        [DataMember] public int Format { get; set; }

        /// <summary>Quando il contenuto è stato modificato l'ultima volta (UTC, formato <see cref="TimeFormat"/>).</summary>
        [DataMember] public string ModifiedUtcText { get; set; }

        [DataMember] public string LastChangeDateText { get; set; }
        [DataMember] public int ValidityDays { get; set; }
        [DataMember] public List<ExchangeEntry> Entries { get; set; }

        /// <summary>Ultimo azzeramento dello storico (UTC): le voci registrate prima vengono eliminate anche sugli altri dispositivi.</summary>
        [DataMember] public string HistoryResetUtcText { get; set; }

        public DateTime? ModifiedUtc
        {
            get { return ParseTime(ModifiedUtcText); }
        }

        public static string FormatTime(DateTime utc)
        {
            return utc.ToUniversalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        public static DateTime? ParseTime(string text)
        {
            DateTime parsed;
            return DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed)
                ? parsed
                : (DateTime?)null;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Entries = Entries ?? new List<ExchangeEntry>();
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(ExchangeData)).WriteObject(stream, this);
                return stream.ToArray();
            }
        }

        public static ExchangeData Deserialize(byte[] plain)
        {
            using (var stream = new MemoryStream(plain))
            {
                var data = (ExchangeData)new DataContractJsonSerializer(typeof(ExchangeData)).ReadObject(stream);
                if (data == null || data.Format != 1)
                {
                    throw new InvalidDataException("Versione del file di scambio non supportata.");
                }

                return data;
            }
        }
    }
}
