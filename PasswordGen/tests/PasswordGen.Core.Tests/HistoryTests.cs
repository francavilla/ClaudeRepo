using System;
using System.IO;
using System.Linq;
using System.Text;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class HistoryTests : IDisposable
    {
        private static readonly DateTime Day = new DateTime(2026, 10, 8);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenHistory_" + Guid.NewGuid().ToString("N"));

        private string FilePath
        {
            get { return Path.Combine(_directory, "history.dat"); }
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>Cifratura reversibile semplice, solo per i test (non usa DPAPI).</summary>
        private sealed class XorProtector : ISecretProtector
        {
            private readonly byte _key;

            public XorProtector(byte key = 0x5A)
            {
                _key = key;
            }

            public byte[] Protect(byte[] data)
            {
                return data.Select(b => (byte)(b ^ _key)).ToArray();
            }

            public byte[] Unprotect(byte[] data)
            {
                return Protect(data);
            }
        }

        // ------------------------------------------------------------ PasswordHistory

        [Fact]
        public void Add_NumbersEntriesAndPutsTheNewestFirst()
        {
            var history = new PasswordHistory();

            var first = history.Add("Uno-Due47!", GenerationMode.Passphrase, Day);
            var second = history.Add("Tre-Quattro83=", GenerationMode.Syllables, Day.AddDays(30));

            Assert.Equal(1, first.Number);
            Assert.Equal(2, second.Number);
            Assert.Equal(new[] { 2, 1 }, history.Entries.Select(e => e.Number).ToArray());
            Assert.Equal(Day.AddDays(30), history.Entries[0].Date);
        }

        [Fact]
        public void Add_WithoutPassword_RecordsOnlyTheDate()
        {
            var history = new PasswordHistory();

            var entry = history.Add(null, GenerationMode.Passphrase, Day);

            Assert.False(entry.HasPassword);
            Assert.Empty(history.Passwords());
        }

        [Fact]
        public void Add_KeepsOnlyTheLatestEntries()
        {
            var history = new PasswordHistory();
            for (var i = 1; i <= PasswordHistory.MaxEntries + 5; i++)
            {
                history.Add("Password" + i, GenerationMode.Random, Day.AddDays(i));
            }

            Assert.Equal(PasswordHistory.MaxEntries, history.Entries.Count);
            Assert.Equal(PasswordHistory.MaxEntries + 5, history.Entries[0].Number);
            Assert.Equal(6, history.Entries.Last().Number);
        }

        [Fact]
        public void Find_MatchesOnlyTheIdenticalPassword()
        {
            var history = new PasswordHistory();
            history.Add("Alfa-Beta-12!", GenerationMode.Passphrase, Day);
            history.Add(null, GenerationMode.Passphrase, Day);

            Assert.NotNull(history.Find("Alfa-Beta-12!"));
            Assert.Null(history.Find("alfa-beta-12!"));
            Assert.Null(history.Find("Alfa-Beta-12"));
            Assert.Null(history.Find(""));
            Assert.Equal(20, PasswordHistory.MaxEntries);
        }

        [Fact]
        public void Merge_StessoGiorno_OrdinaPerMomentoDiRegistrazione()
        {
            var history = new PasswordHistory();
            history.Add("Locale-1111-aaaa!", GenerationMode.Random, Day, new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc));

            history.Merge(new[]
            {
                new HistoryEntry { DateText = "2026-10-08", Password = "Tarda-2222-bbbb!", AddedUtcText = "2026-10-08T15:00:00Z" },
                new HistoryEntry { DateText = "2026-10-08", Password = "Presto-3333-cccc!", AddedUtcText = "2026-10-08T07:00:00Z" },
                new HistoryEntry { DateText = "2026-10-08", Password = "Vecchia-4444-dddd!" }
            });

            Assert.Equal(
                new[] { "Tarda-2222-bbbb!", "Locale-1111-aaaa!", "Presto-3333-cccc!", "Vecchia-4444-dddd!" },
                history.Entries.Select(e => e.Password).ToArray());
        }

        [Fact]
        public void Remove_NeverReusesANumber()
        {
            var history = new PasswordHistory();
            history.Add("A1", GenerationMode.Random, Day);
            history.Add("B2", GenerationMode.Random, Day);

            Assert.True(history.Remove(2));
            Assert.False(history.Remove(2));
            var next = history.Add("C3", GenerationMode.Random, Day);

            Assert.Equal(3, next.Number);
        }

        [Fact]
        public void Clear_EmptiesAndRestartsTheNumbering()
        {
            var history = new PasswordHistory();
            history.Add("A1", GenerationMode.Random, Day);

            history.Clear();

            Assert.Empty(history.Entries);
            Assert.Equal(1, history.Add("B2", GenerationMode.Random, Day).Number);
        }

        // ------------------------------------------------------------ HistoryStore

        [Fact]
        public void Load_WithoutFile_ReturnsEmptyHistory()
        {
            var store = new HistoryStore(FilePath, new XorProtector());

            Assert.Empty(store.Load().Entries);
            Assert.False(store.Exists);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            var store = new HistoryStore(FilePath, new XorProtector());
            var history = new PasswordHistory();
            history.Add("Lampo-Cavallo-Nebbia-Fiume47!", GenerationMode.Passphrase, Day);
            history.Add(null, GenerationMode.Passphrase, Day.AddDays(30));

            store.Save(history);
            var loaded = store.Load();

            Assert.Equal(2, loaded.Entries.Count);
            Assert.Equal(2, loaded.Entries[0].Number);
            Assert.False(loaded.Entries[0].HasPassword);
            Assert.Equal("Lampo-Cavallo-Nebbia-Fiume47!", loaded.Entries[1].Password);
            Assert.Equal(GenerationMode.Passphrase, loaded.Entries[1].Mode);
            Assert.Equal(Day, loaded.Entries[1].Date);
            Assert.Equal(3, loaded.Add("X", GenerationMode.Random, Day).Number);
        }

        [Fact]
        public void SavedFile_DoesNotContainThePasswordInClear()
        {
            var store = new HistoryStore(FilePath, new XorProtector());
            var history = new PasswordHistory();
            history.Add("Lampo-Cavallo-Nebbia-Fiume47!", GenerationMode.Passphrase, Day);

            store.Save(history);

            var raw = Encoding.UTF8.GetString(File.ReadAllBytes(FilePath));
            Assert.DoesNotContain("Lampo", raw);
            Assert.DoesNotContain("Cavallo", raw);
        }

        [Fact]
        public void Load_WithCorruptFile_ReturnsEmptyHistory()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllBytes(FilePath, new byte[] { 1, 2, 3, 4, 5 });

            Assert.Empty(new HistoryStore(FilePath, new XorProtector()).Load().Entries);
        }

        [Fact]
        public void Load_WithAnotherKey_ReturnsEmptyHistory()
        {
            var history = new PasswordHistory();
            history.Add("Lampo-Cavallo47!", GenerationMode.Passphrase, Day);
            new HistoryStore(FilePath, new XorProtector(0x11)).Save(history);

            Assert.Empty(new HistoryStore(FilePath, new XorProtector(0x22)).Load().Entries);
        }

        [Fact]
        public void Save_OverwritesAndLeavesNoTemporaryFile()
        {
            var store = new HistoryStore(FilePath, new XorProtector());
            var history = new PasswordHistory();
            history.Add("A1", GenerationMode.Random, Day);
            store.Save(history);
            history.Add("B2", GenerationMode.Random, Day);
            store.Save(history);

            Assert.Equal(2, store.Load().Entries.Count);
            Assert.False(File.Exists(FilePath + ".tmp"));
        }

        [Fact]
        public void Delete_RemovesTheFile()
        {
            var store = new HistoryStore(FilePath, new XorProtector());
            store.Save(new PasswordHistory());
            Assert.True(store.Exists);

            store.Delete();

            Assert.False(store.Exists);
            store.Delete();   // idempotente
        }

        // ------------------------------------------------------------ DPAPI (Windows)

        [Fact]
        public void DpapiProtector_RoundTripsAndHidesTheContent()
        {
            var protector = new DpapiProtector();
            var plain = Encoding.UTF8.GetBytes("Lampo-Cavallo-Nebbia-Fiume47!");

            var protectedBytes = protector.Protect(plain);

            Assert.DoesNotContain("Lampo", Encoding.UTF8.GetString(protectedBytes));
            Assert.Equal(plain, protector.Unprotect(protectedBytes));
        }

        [Fact]
        public void DpapiStore_RoundTrips()
        {
            var store = new HistoryStore(FilePath, new DpapiProtector());
            var history = new PasswordHistory();
            history.Add("Lampo-Cavallo47!", GenerationMode.Passphrase, Day);

            store.Save(history);

            Assert.Equal("Lampo-Cavallo47!", store.Load().Entries.Single().Password);
        }
    }
}
