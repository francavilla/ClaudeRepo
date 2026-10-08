using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Randomness;
using PasswordGen.Core.Security;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using PasswordGen.Services;
using PasswordGen.ViewModels;
using Xunit;

namespace PasswordGen.App.Tests
{
    public class MainViewModelTests : IDisposable
    {
        private const string Phrase = "frase segreta di prova";
        private static readonly DateTime Today = new DateTime(2026, 10, 8);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "PasswordGenApp_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        // ---- finte dipendenze ----

        private sealed class FakeDialogs : IDialogService
        {
            public bool ConfirmAnswer = true;
            public string SaveFilePath;
            public string OpenFilePath;
            public readonly Queue<string> Passphrases = new Queue<string>();
            public readonly List<string> Asked = new List<string>();

            public bool Confirm(string message, string title) { return ConfirmAnswer; }

            public string PickFile(string title, string filter) { return OpenFilePath; }

            public string PickSaveFile(string title, string filter, string fileName, bool confirmOverwrite) { return SaveFilePath; }

            public string AskPassphrase(string title, string message, bool confirm)
            {
                Asked.Add(message);
                return Passphrases.Count > 0 ? Passphrases.Dequeue() : null;
            }
        }

        private sealed class FakeClipboard : ISecretClipboard
        {
            public TimeSpan ClearAfter { get { return TimeSpan.FromSeconds(30); } }
            public event EventHandler Cleared { add { } remove { } }
            public bool Copy(string text) { return true; }
            public void ClearIfPending() { }
        }

        private sealed class FakeStartup : IStartupRegistration
        {
            public bool IsEnabled { get { return false; } }
            public void SetEnabled(bool enabled) { }
        }

        private sealed class FakeHello : IWindowsHello
        {
            public Task<HelloResult> CheckAvailabilityAsync() { return Task.FromResult(HelloResult.Ok); }
            public Task<HelloResult> AuthenticateAsync(IntPtr window, string message) { return Task.FromResult(HelloResult.Ok); }
        }

        private sealed class XorProtector : ISecretProtector
        {
            public byte[] Protect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
            public byte[] Unprotect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
        }

        private sealed class Harness
        {
            public MainViewModel ViewModel;
            public FakeDialogs Dialogs;
            public SettingsStore Settings;
            public SyncPassphraseStore Passphrases;
            public HistoryStore History;
        }

        private Harness CreateViewModel(string name, AppSettings initial = null)
        {
            var folder = Path.Combine(_directory, name);
            Directory.CreateDirectory(folder);
            var settings = new SettingsStore(Path.Combine(folder, "settings.json"));
            if (initial != null)
            {
                settings.Save(initial);
            }

            var words = WordList.LoadItalian();
            var dialogs = new FakeDialogs();
            var history = new HistoryStore(Path.Combine(folder, "history.dat"), new XorProtector());
            var passphrases = new SyncPassphraseStore(Path.Combine(folder, "sync.key"), new XorProtector());
            var appLock = new AppLockController(new AppLockState(TimeSpan.FromSeconds(30)), new FakeHello(), () => IntPtr.Zero);

            var viewModel = new MainViewModel(new PasswordGenerator(new SecureRandom(), words), words, settings, new FakeClipboard(),
                new FakeStartup(), history, dialogs, appLock, passphrases, () => Today);

            return new Harness { ViewModel = viewModel, Dialogs = dialogs, Settings = settings, Passphrases = passphrases, History = history };
        }

        private static void WaitFor(Func<bool> condition, string what)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.Elapsed > TimeSpan.FromSeconds(60))
                {
                    throw new TimeoutException("Tempo scaduto in attesa di: " + what);
                }

                Thread.Sleep(25);
            }
        }

        private static void RegisterChange(MainViewModel vm, int choice)
        {
            vm.MarkChangedCommand.Execute(null);
            vm.ChoiceIndex = choice;
            vm.ConfirmChangeCommand.Execute(null);
        }

        // ---- cambio password e storico ----

        [Fact]
        public void CambioConProposta_SalvaNelloStoricoEAggiornaLaData()
        {
            var h = CreateViewModel("a");

            RegisterChange(h.ViewModel, 1);

            Assert.Single(h.ViewModel.HistoryEntries);
            Assert.True(h.ViewModel.HasHistory);
            Assert.DoesNotContain("Nessun cambio", h.ViewModel.LastChangeText);
            Assert.Equal(Today, h.Settings.Load().LastChangeDate);
        }

        [Fact]
        public void CambioSoloData_RegistraLaVoceSenzaPassword()
        {
            var h = CreateViewModel("a");

            RegisterChange(h.ViewModel, 0);

            Assert.Single(h.ViewModel.HistoryEntries);
            Assert.False(h.History.Load().Entries[0].HasPassword);
        }

        [Fact]
        public void StoricoDisattivato_ILCambioRegistraSoloLaData()
        {
            var h = CreateViewModel("a", new AppSettings { HistoryEnabled = false });

            h.ViewModel.MarkChangedCommand.Execute(null);

            Assert.False(h.ViewModel.IsChoosing);
            Assert.Empty(h.ViewModel.HistoryEntries);
            Assert.Equal(Today, h.Settings.Load().LastChangeDate);
        }

        // ---- avviso per attivare il blocco ----

        [Fact]
        public void AvvisoBlocco_ComparePoiSiChiudeEResta()
        {
            var h = CreateViewModel("a");
            Assert.True(h.ViewModel.ShowLockHint);

            h.ViewModel.DismissLockHintCommand.Execute(null);

            Assert.False(h.ViewModel.ShowLockHint);
            Assert.True(h.Settings.Load().LockHintDismissed);
            Assert.False(CreateViewModel("a").ViewModel.ShowLockHint);
        }

        // ---- sincronizzazione ----

        [Fact]
        public void ImpostaSincronizzazione_NuovoFile_LoCreaEAttivaLaSincronizzazione()
        {
            var h = CreateViewModel("a");
            RegisterChange(h.ViewModel, 1);
            var file = Path.Combine(_directory, "drive", "sync.pgx");
            h.Dialogs.SaveFilePath = file;
            h.Dialogs.Passphrases.Enqueue(Phrase);

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.Settings.Load().SyncPath == file, "salvataggio delle impostazioni di sincronizzazione");

            Assert.True(h.ViewModel.SyncActive);
            Assert.True(File.Exists(file));
            Assert.Equal(file, h.Settings.Load().SyncPath);
            Assert.Equal(Phrase, h.Passphrases.Load());
            Assert.Contains(file, h.ViewModel.SyncSummary);
            Assert.Single(ExchangeFile.Decrypt(File.ReadAllBytes(file), Phrase).Entries);
        }

        [Fact]
        public void ImpostaSincronizzazione_FraseSbagliata_NonAttivaNulla()
        {
            var file = Path.Combine(_directory, "drive", "sync.pgx");
            var other = new PasswordHistory();
            other.Add("Aaaa-1111-bbbb!", GenerationMode.Passphrase, Today);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, ExchangeFile.Export(other, new AppSettings(), DateTime.UtcNow, "un'altra frase segreta"));

            var h = CreateViewModel("a");
            h.Dialogs.SaveFilePath = file;
            h.Dialogs.Passphrases.Enqueue(Phrase);

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("frase segreta"), "messaggio di frase errata");

            Assert.False(h.ViewModel.SyncActive);
            Assert.Null(h.Passphrases.Load());
            Assert.Null(h.Settings.Load().SyncPath);
        }

        [Fact]
        public void DuePc_SiScambianoLoStoricoTramiteIlFile()
        {
            var file = Path.Combine(_directory, "drive", "sync.pgx");

            var a = CreateViewModel("a");
            RegisterChange(a.ViewModel, 1);
            a.Dialogs.SaveFilePath = file;
            a.Dialogs.Passphrases.Enqueue(Phrase);
            a.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => a.ViewModel.SyncActive, "attivazione sul primo dispositivo");

            var b = CreateViewModel("b");
            b.Dialogs.SaveFilePath = file;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => b.ViewModel.SyncActive, "attivazione sul secondo dispositivo");

            Assert.Single(b.ViewModel.HistoryEntries);
            Assert.Equal(a.ViewModel.HistoryEntries[0].Password, b.ViewModel.HistoryEntries[0].Password);
            Assert.Contains("già", b.Dialogs.Asked.Single());   // il file esisteva: chiesta la frase una volta
        }

        [Fact]
        public void SincronizzaOra_RiportaLeNovitaDellAltroDispositivo()
        {
            var file = Path.Combine(_directory, "drive", "sync.pgx");
            var a = CreateViewModel("a");
            a.Dialogs.SaveFilePath = file;
            a.Dialogs.Passphrases.Enqueue(Phrase);
            a.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => a.ViewModel.SyncActive, "attivazione sul primo dispositivo");

            var b = CreateViewModel("b");
            b.Dialogs.SaveFilePath = file;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => b.ViewModel.SyncActive, "attivazione sul secondo dispositivo");

            RegisterChange(b.ViewModel, 1);   // il cambio sul secondo dispositivo si sincronizza da solo
            WaitFor(() => ExchangeFile.Decrypt(File.ReadAllBytes(file), Phrase).Entries.Count == 1, "scrittura del file");

            a.ViewModel.SyncNowCommand.Execute(null);
            WaitFor(() => a.ViewModel.HistoryEntries.Count == 1, "arrivo della voce sul primo dispositivo");

            Assert.Equal(b.ViewModel.HistoryEntries[0].Password, a.ViewModel.HistoryEntries[0].Password);
        }

        [Fact]
        public void Disattiva_ToglieFileEFrase_EildFileResta()
        {
            var file = Path.Combine(_directory, "drive", "sync.pgx");
            var h = CreateViewModel("a");
            h.Dialogs.SaveFilePath = file;
            h.Dialogs.Passphrases.Enqueue(Phrase);
            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.SyncActive, "attivazione");

            h.ViewModel.StopSyncCommand.Execute(null);

            Assert.False(h.ViewModel.SyncActive);
            Assert.Null(h.Passphrases.Load());
            Assert.Null(h.Settings.Load().SyncPath);
            Assert.True(File.Exists(file));
        }

        [Fact]
        public void SenzaStorico_LaSincronizzazioneNonParte()
        {
            var h = CreateViewModel("a", new AppSettings { HistoryEnabled = false });
            h.Dialogs.SaveFilePath = Path.Combine(_directory, "drive", "sync.pgx");

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("storico"), "messaggio sullo storico");

            Assert.False(h.ViewModel.SyncActive);
        }

        // ---- esporta e importa ----

        [Fact]
        public void EsportaEImporta_RiportaLoStoricoSuUnAltroPc()
        {
            var backup = Path.Combine(_directory, "backup.pgx");
            var a = CreateViewModel("a");
            RegisterChange(a.ViewModel, 1);
            a.Dialogs.SaveFilePath = backup;
            a.Dialogs.Passphrases.Enqueue(Phrase);
            a.ViewModel.ExportCommand.Execute(null);
            WaitFor(() => File.Exists(backup), "file esportato");

            var b = CreateViewModel("b");
            b.Dialogs.OpenFilePath = backup;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.ViewModel.ImportCommand.Execute(null);
            WaitFor(() => b.ViewModel.HistoryEntries.Count == 1, "importazione");

            Assert.Equal(a.ViewModel.HistoryEntries[0].Password, b.ViewModel.HistoryEntries[0].Password);
            Assert.Equal(Today, b.Settings.Load().LastChangeDate);
        }

        [Fact]
        public void Importa_FraseSbagliata_NonModificaLoStorico()
        {
            Directory.CreateDirectory(_directory);
            var backup = Path.Combine(_directory, "backup.pgx");
            var other = new PasswordHistory();
            other.Add("Aaaa-1111-bbbb!", GenerationMode.Passphrase, Today);
            File.WriteAllBytes(backup, ExchangeFile.Export(other, new AppSettings(), DateTime.UtcNow, "un'altra frase segreta"));

            var h = CreateViewModel("a");
            h.Dialogs.OpenFilePath = backup;
            h.Dialogs.Passphrases.Enqueue(Phrase);
            h.ViewModel.ImportCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("frase segreta"), "messaggio di frase errata");

            Assert.Empty(h.ViewModel.HistoryEntries);
        }
    }
}
