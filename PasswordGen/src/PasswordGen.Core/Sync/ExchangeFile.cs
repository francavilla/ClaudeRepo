using System;
using System.Collections.Generic;
using System.Linq;
using PasswordGen.Core.History;
using PasswordGen.Core.Settings;

namespace PasswordGen.Core.Sync
{
    /// <summary>Costruisce, cifra e unisce il file di scambio: è la base di esportazione, importazione e sincronizzazione.</summary>
    public static class ExchangeFile
    {
        public const int MinPassphraseLength = 8;

        /// <summary>Null se la frase va bene, altrimenti il motivo.</summary>
        public static string ValidatePassphrase(string passphrase)
        {
            if (string.IsNullOrEmpty(passphrase) || passphrase.Length < MinPassphraseLength)
            {
                return "La frase segreta deve avere almeno " + MinPassphraseLength + " caratteri.";
            }

            return null;
        }

        /// <summary>Contenuto da scrivere, a partire dallo stato locale.</summary>
        public static ExchangeData Build(PasswordHistory history, AppSettings settings, string modifiedUtcText)
        {
            var data = new ExchangeData
            {
                ModifiedUtcText = modifiedUtcText,
                LastChangeDateText = settings.LastChangeDateText,
                ValidityDays = settings.ValidityDays
            };

            foreach (var entry in history.Entries)
            {
                data.Entries.Add(new ExchangeEntry { DateText = entry.DateText, Mode = entry.Mode, Password = entry.Password ?? string.Empty });
            }

            return data;
        }

        /// <summary>Esporta lo stato locale in un file cifrato con la frase segreta.</summary>
        public static byte[] Export(PasswordHistory history, AppSettings settings, DateTime nowUtc, string passphrase,
            int iterations = PassphraseProtector.DefaultIterations)
        {
            var data = Build(history, settings, ExchangeData.FormatTime(nowUtc));
            return new PassphraseProtector(passphrase, iterations).Protect(data.Serialize());
        }

        /// <summary>
        /// Unisce nello stato locale il contenuto di un file di scambio. Lo storico si unisce senza perdere nulla;
        /// la data dell'ultimo cambio è la più recente delle due; la durata della password si prende dal file se
        /// <paramref name="preferIncomingValidity"/> è vero.
        /// </summary>
        public static MergeSummary Merge(ExchangeData incoming, PasswordHistory history, AppSettings settings, bool preferIncomingValidity)
        {
            var summary = new MergeSummary();
            summary.EntriesAdded = history.Merge(incoming.Entries.Select(ToHistoryEntry));

            DateTime parsed;
            if (DateTime.TryParseExact(incoming.LastChangeDateText, AppSettings.DateFormat,
                    System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
            {
                var local = settings.LastChangeDate;
                if (!local.HasValue || parsed > local.Value)
                {
                    settings.LastChangeDate = parsed;
                    summary.SettingsChanged = true;
                }
            }

            if (preferIncomingValidity && incoming.ValidityDays >= 7 && incoming.ValidityDays <= 365
                && incoming.ValidityDays != settings.ValidityDays)
            {
                settings.ValidityDays = incoming.ValidityDays;
                summary.SettingsChanged = true;
            }

            return summary;
        }

        /// <summary>Importa un file cifrato (unione con lo stato locale). Eccezioni: vedi <see cref="Decrypt"/>.</summary>
        public static MergeSummary Import(byte[] file, string passphrase, PasswordHistory history, AppSettings settings,
            int iterations = PassphraseProtector.DefaultIterations)
        {
            return Merge(Decrypt(file, passphrase, iterations), history, settings, true);
        }

        /// <exception cref="System.IO.InvalidDataException">Non è un file di scambio di PasswordGen.</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">Frase errata o file alterato.</exception>
        public static ExchangeData Decrypt(byte[] file, string passphrase, int iterations = PassphraseProtector.DefaultIterations)
        {
            return Decrypt(file, new PassphraseProtector(passphrase, iterations));
        }

        /// <summary>Come <see cref="Decrypt(byte[], string, int)"/>, ma con un protettore da riusare per riscrivere il file (una sola derivazione).</summary>
        public static ExchangeData Decrypt(byte[] file, PassphraseProtector protector)
        {
            var plain = protector.Unprotect(file);
            try
            {
                return ExchangeData.Deserialize(plain);
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                throw new System.IO.InvalidDataException("Il file di scambio non è valido.", ex);
            }
        }

        private static HistoryEntry ToHistoryEntry(ExchangeEntry entry)
        {
            return new HistoryEntry { DateText = entry.DateText, Mode = entry.Mode, Password = entry.Password };
        }
    }

    public sealed class MergeSummary
    {
        /// <summary>Voci di storico nuove arrivate dal file.</summary>
        public int EntriesAdded { get; set; }

        /// <summary>La data dell'ultimo cambio o la durata della password sono cambiate.</summary>
        public bool SettingsChanged { get; set; }
    }
}
