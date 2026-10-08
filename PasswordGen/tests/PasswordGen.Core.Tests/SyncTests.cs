using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Security;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class SyncTests : IDisposable
    {
        private const int Fast = 1000;   // iterazioni ridotte: i test restano veloci
        private const string Phrase = "frase segreta di prova";
        private static readonly DateTime Now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenSync_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private sealed class MemoryStorage : ISyncStorage
        {
            public byte[] Data;
            public int Writes;
            public bool FailRead;

            public byte[] Read()
            {
                if (FailRead)
                {
                    throw new IOException("disco non disponibile");
                }

                return Data;
            }

            public void Write(byte[] data)
            {
                Data = data;
                Writes++;
            }
        }

        private sealed class XorProtector : ISecretProtector
        {
            public byte[] Protect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
            public byte[] Unprotect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
        }

        private static PasswordHistory HistoryWith(params string[] dateAndPassword)
        {
            var history = new PasswordHistory();
            foreach (var item in dateAndPassword)
            {
                var parts = item.Split('|');
                history.Add(parts[1], GenerationMode.Passphrase, DateTime.ParseExact(parts[0], "yyyy-MM-dd", null));
            }

            return history;
        }

        // ---- PBKDF2 ----

        private static string Hex(byte[] bytes)
        {
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        [Fact]
        public void Pbkdf2_VettoriDiProva_Standard()
        {
            var salt = Encoding.UTF8.GetBytes("salt");
            Assert.Equal("120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b", Hex(Pbkdf2.Derive("password", salt, 1, 32)));
            Assert.Equal("ae4d0c95af6b46d32d0adff928f06dd02a303f8ef3c251dfd6e2d85a95474c43", Hex(Pbkdf2.Derive("password", salt, 2, 32)));
            Assert.Equal("c5e478d59288c841aa530db6845c4c8d962893a001ce4e11a4963873aa98134a", Hex(Pbkdf2.Derive("password", salt, 4096, 32)));
        }

        [Fact]
        public void Pbkdf2_PiuBlocchi_Standard()
        {
            var salt = Encoding.UTF8.GetBytes("saltSALTsaltSALTsaltSALTsaltSALTsalt");
            Assert.Equal(
                "348c89dbcbd32b2f32d814b8116e84cf2b17347ebc1800181c4e2a1fb8dd53e1c635518c7dac47e9",
                Hex(Pbkdf2.Derive("passwordPASSWORDpassword", salt, 4096, 40)));
        }

        // ---- cifratura con frase ----

        [Fact]
        public void PassphraseProtector_CifraEDecifra()
        {
            var data = Encoding.UTF8.GetBytes("dati riservati");
            var encrypted = new PassphraseProtector(Phrase, Fast).Protect(data);

            Assert.NotEqual(data, encrypted);
            Assert.Equal(data, new PassphraseProtector(Phrase, Fast).Unprotect(encrypted));
        }

        [Fact]
        public void PassphraseProtector_FraseSbagliata_Rifiuta()
        {
            var encrypted = new PassphraseProtector(Phrase, Fast).Protect(new byte[] { 1, 2, 3 });
            Assert.Throws<CryptographicException>(() => new PassphraseProtector("altra frase", Fast).Unprotect(encrypted));
        }

        [Fact]
        public void PassphraseProtector_FileAlterato_Rifiuta()
        {
            var encrypted = new PassphraseProtector(Phrase, Fast).Protect(new byte[] { 1, 2, 3 });
            encrypted[encrypted.Length - 40] ^= 1;
            Assert.Throws<CryptographicException>(() => new PassphraseProtector(Phrase, Fast).Unprotect(encrypted));
        }

        [Fact]
        public void PassphraseProtector_NonEUnFileDiScambio_Rifiuta()
        {
            Assert.Throws<InvalidDataException>(() => new PassphraseProtector(Phrase, Fast).Unprotect(Encoding.UTF8.GetBytes("questo non è un file cifrato di PasswordGen")));
            Assert.Throws<InvalidDataException>(() => new PassphraseProtector(Phrase, Fast).Unprotect(new byte[3]));
        }

        [Fact]
        public void PassphraseProtector_SaleDiversoAdOgniScrittura()
        {
            var protector = new PassphraseProtector(Phrase, Fast);
            var data = new byte[] { 9, 9, 9 };
            Assert.NotEqual(protector.Protect(data), protector.Protect(data));
        }

        [Fact]
        public void PassphraseProtector_IterazioniNelFile_ChiHaCifratoConAltreIterazioniSiApreComunque()
        {
            var encrypted = new PassphraseProtector(Phrase, 2000).Protect(new byte[] { 5 });
            Assert.Equal(new byte[] { 5 }, new PassphraseProtector(Phrase, Fast).Unprotect(encrypted));
        }

        [Fact]
        public void PassphraseProtector_FraseVuota_Eccezione()
        {
            Assert.Throws<ArgumentException>(() => new PassphraseProtector("", Fast));
        }

        [Fact]
        public void ValidatePassphrase_RichiedeAlmenoOttoCaratteri()
        {
            Assert.NotNull(ExchangeFile.ValidatePassphrase(null));
            Assert.NotNull(ExchangeFile.ValidatePassphrase("corta"));
            Assert.Null(ExchangeFile.ValidatePassphrase("abcdefgh"));
        }

        // ---- unione dello storico ----

        [Fact]
        public void Merge_AggiungeVociNuoveEOrdinaPerData()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var added = history.Merge(new[]
            {
                new HistoryEntry { DateText = "2026-10-01", Password = "Cccc-2222-dddd!" },
                new HistoryEntry { DateText = "2026-08-01", Password = "Eeee-3333-ffff!" }
            });

            Assert.Equal(2, added);
            Assert.Equal(new[] { "2026-10-01", "2026-09-01", "2026-08-01" }, history.Entries.Select(e => e.DateText).ToArray());
        }

        [Fact]
        public void Merge_VoceGiaPresente_NonSiDuplica()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var added = history.Merge(new[] { new HistoryEntry { DateText = "2026-09-01", Password = "Aaaa-1111-bbbb!" } });

            Assert.Equal(0, added);
            Assert.Single(history.Entries);
        }

        [Fact]
        public void Merge_StessoGiornoPasswordDiverse_SiTengonoEntrambe()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            history.Merge(new[] { new HistoryEntry { DateText = "2026-09-01", Password = "Zzzz-9999-yyyy!" } });

            Assert.Equal(2, history.Entries.Count);
        }

        [Fact]
        public void Merge_SoloData_AssorbitaDaVoceDelloStessoGiorno()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var added = history.Merge(new[] { new HistoryEntry { DateText = "2026-09-01", Password = "" } });

            Assert.Equal(0, added);
            Assert.Single(history.Entries);
        }

        [Fact]
        public void Merge_VoceConPassword_SostituisceLaSolaData()
        {
            var history = HistoryWith("2026-09-01|");
            history.Merge(new[] { new HistoryEntry { DateText = "2026-09-01", Password = "Aaaa-1111-bbbb!" } });

            Assert.Single(history.Entries);
            Assert.True(history.Entries[0].HasPassword);
        }

        [Fact]
        public void Merge_DateNonValide_Ignorate()
        {
            var history = new PasswordHistory();
            var added = history.Merge(new[]
            {
                new HistoryEntry { DateText = "ieri", Password = "x" },
                new HistoryEntry { DateText = null, Password = "y" },
                null
            });

            Assert.Equal(0, added);
            Assert.Empty(history.Entries);
        }

        [Fact]
        public void Merge_RispettaIlLimiteDelloStorico_TengaLePiuRecenti()
        {
            var history = new PasswordHistory();
            var incoming = Enumerable.Range(1, 20)
                .Select(i => new HistoryEntry { DateText = new DateTime(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd"), Password = "Pw-" + i + "-abcdef!" })
                .ToList();

            history.Merge(incoming);

            Assert.Equal(PasswordHistory.MaxEntries, history.Entries.Count);
            Assert.Equal(new DateTime(2026, 1, 21).ToString("yyyy-MM-dd"), history.Entries[0].DateText);
        }

        [Fact]
        public void Merge_NumeriProgressiviNuoviENonRiutilizzati()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            history.Merge(new[] { new HistoryEntry { DateText = "2026-10-01", Password = "Cccc-2222-dddd!", Mode = GenerationMode.Syllables } });

            Assert.Equal(new[] { 2, 1 }, history.Entries.Select(e => e.Number).ToArray());
            Assert.Equal(GenerationMode.Syllables, history.Entries[0].Mode);
        }

        // ---- esportazione e importazione ----

        [Fact]
        public void EsportaEImporta_RiportaStoricoDataEDurata()
        {
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!", "2026-10-01|Cccc-2222-dddd!");
            var settings = new AppSettings { ValidityDays = 45 };
            settings.LastChangeDate = new DateTime(2026, 10, 1);

            var file = ExchangeFile.Export(history, settings, Now, Phrase, Fast);

            var otherHistory = new PasswordHistory();
            var otherSettings = new AppSettings();
            var summary = ExchangeFile.Import(file, Phrase, otherHistory, otherSettings, Fast);

            Assert.Equal(2, summary.EntriesAdded);
            Assert.True(summary.SettingsChanged);
            Assert.Equal(new[] { "Cccc-2222-dddd!", "Aaaa-1111-bbbb!" }, otherHistory.Passwords().ToArray());
            Assert.Equal(new DateTime(2026, 10, 1), otherSettings.LastChangeDate);
            Assert.Equal(45, otherSettings.ValidityDays);
        }

        [Fact]
        public void Importa_FraseSbagliata_Eccezione_ENessunaModifica()
        {
            var file = ExchangeFile.Export(HistoryWith("2026-09-01|Aaaa-1111-bbbb!"), new AppSettings(), Now, Phrase, Fast);
            var history = new PasswordHistory();

            Assert.Throws<CryptographicException>(() => ExchangeFile.Import(file, "un'altra frase", history, new AppSettings(), Fast));
            Assert.Empty(history.Entries);
        }

        [Fact]
        public void Importa_DataUltimoCambio_PrendeLaPiuRecente()
        {
            var settings = new AppSettings();
            settings.LastChangeDate = new DateTime(2026, 11, 1);
            var older = new AppSettings();
            older.LastChangeDate = new DateTime(2026, 9, 1);
            var file = ExchangeFile.Export(new PasswordHistory(), older, Now, Phrase, Fast);

            ExchangeFile.Import(file, Phrase, new PasswordHistory(), settings, Fast);

            Assert.Equal(new DateTime(2026, 11, 1), settings.LastChangeDate);
        }

        [Fact]
        public void IlFileEsportatoNonContieneLePasswordInChiaro()
        {
            var file = ExchangeFile.Export(HistoryWith("2026-09-01|Aaaa-1111-bbbb!"), new AppSettings(), Now, Phrase, Fast);
            Assert.DoesNotContain("Aaaa-1111-bbbb", Encoding.UTF8.GetString(file));
        }

        // ---- sincronizzazione ----

        [Fact]
        public void Sync_FileNuovo_LoCreaConIDatiLocali()
        {
            var storage = new MemoryStorage();
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var settings = new AppSettings();

            var result = SyncEngine.Run(storage, Phrase, history, settings, Now, Fast);

            Assert.True(result.Succeeded);
            Assert.True(result.Wrote);
            Assert.Equal(1, storage.Writes);
            Assert.Equal(ExchangeData.FormatTime(Now), settings.LastSyncUtcText);
            Assert.Single(ExchangeFile.Decrypt(storage.Data, Phrase, Fast).Entries);
        }

        [Fact]
        public void Sync_DueDispositivi_SiScambianoLeVoci()
        {
            var storage = new MemoryStorage();
            var historyA = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var settingsA = new AppSettings();
            SyncEngine.Run(storage, Phrase, historyA, settingsA, Now, Fast);

            var historyB = HistoryWith("2026-10-01|Cccc-2222-dddd!");
            var settingsB = new AppSettings();
            var resultB = SyncEngine.Run(storage, Phrase, historyB, settingsB, Now.AddMinutes(5), Fast);

            Assert.Equal(1, resultB.EntriesAdded);
            Assert.Equal(2, historyB.Entries.Count);

            var resultA = SyncEngine.Run(storage, Phrase, historyA, settingsA, Now.AddMinutes(10), Fast);
            Assert.Equal(1, resultA.EntriesAdded);
            Assert.Equal(historyB.Passwords().ToArray(), historyA.Passwords().ToArray());
        }

        [Fact]
        public void Sync_NienteDiNuovo_NonRiscriveIlFile()
        {
            var storage = new MemoryStorage();
            var history = HistoryWith("2026-09-01|Aaaa-1111-bbbb!");
            var settings = new AppSettings();
            SyncEngine.Run(storage, Phrase, history, settings, Now, Fast);
            var writes = storage.Writes;

            var result = SyncEngine.Run(storage, Phrase, history, settings, Now.AddMinutes(1), Fast);

            Assert.True(result.Succeeded);
            Assert.False(result.Wrote);
            Assert.Equal(writes, storage.Writes);
            Assert.Equal(ExchangeData.FormatTime(Now.AddMinutes(1)), settings.LastSyncUtcText);
        }

        [Fact]
        public void Sync_FraseSbagliata_NonModificaNulla()
        {
            var storage = new MemoryStorage();
            SyncEngine.Run(storage, Phrase, HistoryWith("2026-09-01|Aaaa-1111-bbbb!"), new AppSettings(), Now, Fast);
            var before = storage.Data.ToArray();
            var history = HistoryWith("2026-10-01|Cccc-2222-dddd!");
            var settings = new AppSettings();

            var result = SyncEngine.Run(storage, "frase sbagliata", history, settings, Now.AddMinutes(1), Fast);

            Assert.Equal(SyncStatus.WrongPassphrase, result.Status);
            Assert.Equal(before, storage.Data);
            Assert.Single(history.Entries);
            Assert.Null(settings.LastSyncUtcText);
        }

        [Fact]
        public void Sync_FileCheNonEDiPasswordGen_Segnala()
        {
            var storage = new MemoryStorage { Data = Encoding.UTF8.GetBytes("altro contenuto qualsiasi, abbastanza lungo da superare l'intestazione") };

            var result = SyncEngine.Run(storage, Phrase, new PasswordHistory(), new AppSettings(), Now, Fast);

            Assert.Equal(SyncStatus.InvalidFile, result.Status);
            Assert.Equal(0, storage.Writes);
        }

        [Fact]
        public void Sync_ErroreDiLettura_Segnala()
        {
            var storage = new MemoryStorage { FailRead = true };

            var result = SyncEngine.Run(storage, Phrase, new PasswordHistory(), new AppSettings(), Now, Fast);

            Assert.Equal(SyncStatus.Error, result.Status);
            Assert.Contains("disco non disponibile", result.Message);
        }

        [Fact]
        public void Sync_Durata_ChiHaModificatoDopoLUltimaSincronizzazioneVince()
        {
            var storage = new MemoryStorage();
            var historyA = new PasswordHistory();
            var settingsA = new AppSettings { ValidityDays = 30 };
            SyncEngine.Run(storage, Phrase, historyA, settingsA, Now, Fast);

            // A cambia la durata e riscrive il file.
            settingsA.ValidityDays = 45;
            SyncEngine.Run(storage, Phrase, historyA, settingsA, Now.AddMinutes(10), Fast);

            // B si era sincronizzato prima: adotta la durata del file.
            var historyB = new PasswordHistory();
            var settingsB = new AppSettings { ValidityDays = 30, LastSyncUtcText = ExchangeData.FormatTime(Now) };
            SyncEngine.Run(storage, Phrase, historyB, settingsB, Now.AddMinutes(20), Fast);

            Assert.Equal(45, settingsB.ValidityDays);
        }

        [Fact]
        public void Sync_Durata_ModificaLocaleDopoLUltimaSincronizzazioneVinceSulFile()
        {
            var storage = new MemoryStorage();
            var settingsA = new AppSettings { ValidityDays = 30 };
            SyncEngine.Run(storage, Phrase, new PasswordHistory(), settingsA, Now, Fast);

            settingsA.ValidityDays = 60;   // modifica locale dopo l'ultima sincronizzazione
            SyncEngine.Run(storage, Phrase, new PasswordHistory(), settingsA, Now.AddMinutes(10), Fast);

            Assert.Equal(60, settingsA.ValidityDays);
            Assert.Equal(60, ExchangeFile.Decrypt(storage.Data, Phrase, Fast).ValidityDays);
        }

        [Fact]
        public void Sync_PrimaSincronizzazione_AdottaLaDurataDelFile()
        {
            var storage = new MemoryStorage();
            SyncEngine.Run(storage, Phrase, new PasswordHistory(), new AppSettings { ValidityDays = 90 }, Now, Fast);

            var settings = new AppSettings { ValidityDays = 30 };
            SyncEngine.Run(storage, Phrase, new PasswordHistory(), settings, Now.AddMinutes(1), Fast);

            Assert.Equal(90, settings.ValidityDays);
        }

        // ---- file reale e frase salvata ----

        [Fact]
        public void FileSyncStorage_LeggeEScrive_ECreaLaCartella()
        {
            var path = Path.Combine(_directory, "sotto", "sync.pgx");
            var storage = new FileSyncStorage(path);

            Assert.Null(storage.Read());
            storage.Write(new byte[] { 1, 2, 3 });
            storage.Write(new byte[] { 4, 5 });

            Assert.Equal(new byte[] { 4, 5 }, storage.Read());
            Assert.False(File.Exists(path + ".tmp"));
        }

        [Fact]
        public void SyncPassphraseStore_SalvaCaricaEElimina()
        {
            var store = new SyncPassphraseStore(Path.Combine(_directory, "sync.key"), new XorProtector());

            Assert.Null(store.Load());
            store.Save(Phrase);
            Assert.Equal(Phrase, store.Load());
            store.Delete();
            Assert.Null(store.Load());
        }

        [Fact]
        public void SyncPassphraseStore_FileNonLeggibile_RestituisceNull()
        {
            var path = Path.Combine(_directory, "sync.key");
            new SyncPassphraseStore(path, new XorProtector()).Save(Phrase);

            Assert.Null(new SyncPassphraseStore(path, new ThrowingProtector()).Load());
        }

        private sealed class ThrowingProtector : ISecretProtector
        {
            public byte[] Protect(byte[] data) { throw new CryptographicException(); }
            public byte[] Unprotect(byte[] data) { throw new CryptographicException(); }
        }

        [Fact]
        public void Impostazioni_NuoviCampi_SiSalvanoESiRileggono()
        {
            var path = Path.Combine(_directory, "settings.json");
            var store = new SettingsStore(path);
            store.Save(new AppSettings { SyncPath = @"G:\Il mio Drive\sync.pgx", LastSyncUtcText = "2026-10-08T12:00:00Z", LockHintDismissed = true });

            var loaded = store.Load();

            Assert.Equal(@"G:\Il mio Drive\sync.pgx", loaded.SyncPath);
            Assert.Equal("2026-10-08T12:00:00Z", loaded.LastSyncUtcText);
            Assert.True(loaded.LockHintDismissed);
            Assert.False(new AppSettings().LockHintDismissed);
        }
    }
}
