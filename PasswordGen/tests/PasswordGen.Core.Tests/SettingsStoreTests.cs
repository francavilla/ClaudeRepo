using System;
using System.IO;
using PasswordGen.Core.Generation;
using PasswordGen.Core.Settings;
using Xunit;

namespace PasswordGen.Core.Tests
{
    public class SettingsStoreTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenTests_" + Guid.NewGuid().ToString("N"));

        private string FilePath
        {
            get { return Path.Combine(_directory, "settings.json"); }
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Fact]
        public void Load_WithoutFile_ReturnsDefaults()
        {
            var settings = new SettingsStore(FilePath).Load();

            Assert.Equal(GenerationMode.Passphrase, settings.Mode);
            Assert.Equal(4, settings.WordCount);
            Assert.Equal(6, settings.SuggestionCount);
            Assert.Equal(10, settings.MinLength);
            Assert.True(settings.HistoryEnabled);
            Assert.False(settings.LockEnabled);
            Assert.Equal(30, settings.LockGraceSeconds);
            Assert.Equal(15, settings.SyncIntervalSeconds);
            Assert.Equal(GenerationMode.Passphrase, settings.Mode);
            Assert.Equal(WordSourceMode.Builtin, settings.WordSource);
            Assert.Null(settings.CustomWordsPath);
            Assert.True(settings.ReminderEnabled);
            Assert.Equal(30, settings.ValidityDays);
            Assert.Null(settings.LastChangeDate);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips()
        {
            var store = new SettingsStore(FilePath);
            var settings = new AppSettings
            {
                Mode = GenerationMode.Random,
                WordCount = 6,
                RandomLength = 20,
                SuggestionCount = 12,
                MinLength = 12,
                RequireSpecial = false,
                HistoryEnabled = false,
                LockEnabled = true,
                LockGraceSeconds = 300,
                WordSource = WordSourceMode.Combined,
                CustomWordsPath = @"C:\dati\parole.txt",
                ValidityDays = 60,
                LastChangeDate = new DateTime(2026, 10, 8)
            };

            store.Save(settings);
            var loaded = store.Load();

            Assert.Equal(GenerationMode.Random, loaded.Mode);
            Assert.Equal(6, loaded.WordCount);
            Assert.Equal(20, loaded.RandomLength);
            Assert.Equal(12, loaded.SuggestionCount);
            Assert.Equal(12, loaded.MinLength);
            Assert.False(loaded.RequireSpecial);
            Assert.False(loaded.HistoryEnabled);
            Assert.True(loaded.LockEnabled);
            Assert.Equal(300, loaded.LockGraceSeconds);
            Assert.Equal(WordSourceMode.Combined, loaded.WordSource);
            Assert.Equal(@"C:\dati\parole.txt", loaded.CustomWordsPath);
            Assert.True(loaded.RequireUpper);
            Assert.Equal(60, loaded.ValidityDays);
            Assert.Equal(new DateTime(2026, 10, 8), loaded.LastChangeDate);
        }

        [Fact]
        public void Load_WithCorruptFile_ReturnsDefaults()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath, "{ questo non è JSON");

            var settings = new SettingsStore(FilePath).Load();

            Assert.Equal(4, settings.WordCount);
        }

        [Fact]
        public void Load_WithMissingFields_KeepsDefaultsForThem()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath, "{\"WordCount\":6}");

            var settings = new SettingsStore(FilePath).Load();

            Assert.Equal(6, settings.WordCount);
            Assert.Equal(10, settings.MinLength);
            Assert.True(settings.RequireDigit);
            Assert.True(settings.ReminderEnabled);
        }

        [Fact]
        public void Load_ClampsOutOfRangeValues()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath, "{\"WordCount\":99,\"MinLength\":1,\"ValidityDays\":0,\"SuggestionCount\":500,\"LockGraceSeconds\":999999}");

            var settings = new SettingsStore(FilePath).Load();

            Assert.Equal(GenerationOptions.MaxWords, settings.WordCount);
            Assert.Equal(6, settings.MinLength);
            Assert.Equal(7, settings.ValidityDays);
            Assert.Equal(AppSettings.MaxSuggestions, settings.SuggestionCount);
            Assert.Equal(PasswordGen.Core.Security.AppLockState.MaxGraceSeconds, settings.LockGraceSeconds);
        }

        [Fact]
        public void Save_OverwritesAnExistingFile()
        {
            var store = new SettingsStore(FilePath);
            store.Save(new AppSettings { WordCount = 5 });
            store.Save(new AppSettings { WordCount = 7 });

            Assert.Equal(7, store.Load().WordCount);
            Assert.False(File.Exists(FilePath + ".tmp"));
        }

        [Fact]
        public void SavedFile_NeverContainsPasswordFields()
        {
            var store = new SettingsStore(FilePath);
            store.Save(new AppSettings());

            var json = File.ReadAllText(FilePath).ToLowerInvariant();

            Assert.DoesNotContain("password\"", json);
        }
    }
}
