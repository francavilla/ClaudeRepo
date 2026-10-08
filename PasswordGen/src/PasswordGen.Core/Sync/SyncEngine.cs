using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using PasswordGen.Core.History;
using PasswordGen.Core.Settings;

namespace PasswordGen.Core.Sync
{
    public enum SyncStatus
    {
        /// <summary>Sincronizzazione riuscita.</summary>
        Done,

        /// <summary>La frase segreta non apre il file (o il file è alterato).</summary>
        WrongPassphrase,

        /// <summary>Il file non è un file di scambio di PasswordGen.</summary>
        InvalidFile,

        /// <summary>Errore di lettura o scrittura del file.</summary>
        Error
    }

    public sealed class SyncResult
    {
        public SyncStatus Status { get; set; }
        public string Message { get; set; }

        /// <summary>Voci di storico arrivate dall'altro dispositivo.</summary>
        public int EntriesAdded { get; set; }

        /// <summary>Il file è stato riscritto con i dati di questo dispositivo.</summary>
        public bool Wrote { get; set; }

        public bool Succeeded
        {
            get { return Status == SyncStatus.Done; }
        }
    }

    /// <summary>
    /// Sincronizzazione dei due dispositivi tramite un file cifrato condiviso (per esempio in Google Drive): si legge il file,
    /// si unisce il suo contenuto con lo stato locale e, se c'è qualcosa di nuovo, lo si riscrive. Non si perde nulla.
    /// Lo stato locale (<paramref name="history"/> e <paramref name="settings"/>) viene modificato: lo salva chi chiama.
    /// </summary>
    public static class SyncEngine
    {
        public static SyncResult Run(ISyncStorage storage, string passphrase, PasswordHistory history, AppSettings settings,
            DateTime nowUtc, int iterations = PassphraseProtector.DefaultIterations)
        {
            var result = new SyncResult { Status = SyncStatus.Done };
            try
            {
                var protector = new PassphraseProtector(passphrase, iterations);
                var raw = storage.Read();
                ExchangeData remote = null;
                if (raw != null && raw.Length > 0)
                {
                    try
                    {
                        remote = ExchangeFile.Decrypt(raw, passphrase, iterations);
                    }
                    catch (CryptographicException)
                    {
                        return Fail(result, SyncStatus.WrongPassphrase, "La frase segreta non è quella del file di sincronizzazione (o il file è stato alterato).");
                    }
                    catch (InvalidDataException ex)
                    {
                        return Fail(result, SyncStatus.InvalidFile, ex.Message);
                    }
                }

                string remoteModified = null;
                if (remote != null)
                {
                    remoteModified = remote.ModifiedUtcText;
                    var lastSync = ExchangeData.ParseTime(settings.LastSyncUtcText);
                    var remoteTime = remote.ModifiedUtc;
                    var preferRemote = !lastSync.HasValue || (remoteTime.HasValue && remoteTime.Value > lastSync.Value);
                    result.EntriesAdded = ExchangeFile.Merge(remote, history, settings, preferRemote).EntriesAdded;
                }

                // Si riscrive solo se il risultato dell'unione differisce dal file: niente scritture inutili per il client di sincronizzazione.
                var merged = ExchangeFile.Build(history, settings, remoteModified);
                var unchanged = remote != null && merged.Serialize().SequenceEqual(remote.Serialize());
                if (!unchanged)
                {
                    merged.ModifiedUtcText = ExchangeData.FormatTime(nowUtc);
                    storage.Write(protector.Protect(merged.Serialize()));
                    result.Wrote = true;
                }

                settings.LastSyncUtcText = ExchangeData.FormatTime(nowUtc);
                return result;
            }
            catch (Exception ex)
            {
                return Fail(result, SyncStatus.Error, "Sincronizzazione non riuscita: " + ex.Message);
            }
        }

        private static SyncResult Fail(SyncResult result, SyncStatus status, string message)
        {
            result.Status = status;
            result.Message = message;
            return result;
        }
    }
}
