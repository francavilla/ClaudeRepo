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
using PasswordGen.Core.Sync.Google;
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

            public readonly Queue<string> Texts = new Queue<string>();

            public string AskText(string title, string message, string initial) { return Texts.Count > 0 ? Texts.Dequeue() : null; }

            public int ChooseIndex = -1;
            public IReadOnlyList<string> ChooseOptions;
            public string NewSecret;
            public string ExistingSecret;

            public int Choose(string title, IReadOnlyList<string> options)
            {
                ChooseOptions = options;
                return ChooseIndex;
            }

            public string AskNewSecret(CredentialKind kind) { return NewSecret; }

            public string AskSecret(string title, string message) { return ExistingSecret; }
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
            public bool Available = true;
            public bool AuthenticationSucceeds = true;
            public int Authentications;

            public Task<HelloResult> CheckAvailabilityAsync()
            {
                return Task.FromResult(Available ? HelloResult.Ok : HelloResult.Failed("Windows Hello non è configurato"));
            }

            public Task<HelloResult> AuthenticateAsync(IntPtr window, string message)
            {
                Authentications++;
                return Task.FromResult(AuthenticationSucceeds ? HelloResult.Ok : HelloResult.Failed("rifiutato"));
            }
        }

        private sealed class XorProtector : ISecretProtector
        {
            public byte[] Protect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
            public byte[] Unprotect(byte[] data) { return data.Select(b => (byte)(b ^ 0x5A)).ToArray(); }
        }

        private sealed class MemoryStorage : ISyncStorage
        {
            public byte[] Data;

            public byte[] Read() { return Data; }

            public void Write(byte[] data) { Data = data; }
        }

        /// <summary>Google Drive finto: si può condividere lo stesso «file» tra due finti dispositivi.</summary>
        private sealed class FakeDrive : IGoogleDriveService
        {
            public bool Configured = true;
            public bool SignedIn;
            public string SignInProblem;
            public int SignIns;
            public int SignOuts;
            public MemoryStorage Storage = new MemoryStorage();

            public string Address { get { return "google-drive:"; } }
            public bool IsConfigured { get { return Configured; } }
            public bool IsSignedIn { get { return SignedIn; } }

            public Task<string> SignInAsync()
            {
                SignIns++;
                if (SignInProblem == null)
                {
                    SignedIn = true;
                }

                return Task.FromResult(SignInProblem);
            }

            public ISyncStorage CreateStorage() { return Storage; }

            public void SignOut()
            {
                SignOuts++;
                SignedIn = false;
            }
        }

        private sealed class Harness
        {
            public MainViewModel ViewModel;
            public FakeDialogs Dialogs;
            public SettingsStore Settings;
            public SyncPassphraseStore Passphrases;
            public HistoryStore History;
            public FakeHello Hello;
            public AppLockController Lock;
            public FakeDrive Drive;
            public LockCredentialManager Credentials;
        }

        private Harness CreateViewModel(string name, AppSettings initial = null, bool helloAvailable = true, FakeDrive drive = null)
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
            drive = drive ?? new FakeDrive();
            var hello = new FakeHello { Available = helloAvailable };
            var credentials = new LockCredentialManager(new LockCredentialStore(Path.Combine(folder, "lock.dat"), new XorProtector()));
            var appLock = new AppLockController(new AppLockState(TimeSpan.FromSeconds(30)), hello, () => IntPtr.Zero, credentials);
            appLock.RefreshAvailabilityAsync().GetAwaiter().GetResult();

            var viewModel = new MainViewModel(new PasswordGenerator(new SecureRandom(), words), words, settings, new FakeClipboard(),
                new FakeStartup(), history, dialogs, appLock, passphrases, drive, () => Today);

            return new Harness { ViewModel = viewModel, Dialogs = dialogs, Settings = settings, Passphrases = passphrases, History = history, Hello = hello, Lock = appLock, Credentials = credentials, Drive = drive };
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
        public void CambioConTitolo_LoSalvaNelloStorico()
        {
            var h = CreateViewModel("a");

            h.ViewModel.MarkChangedCommand.Execute(null);
            h.ViewModel.ChoiceIndex = 1;
            h.ViewModel.ChoiceLabel = "  Portale HR  ";
            h.ViewModel.ConfirmChangeCommand.Execute(null);

            Assert.Equal("Portale HR", h.History.Load().Entries[0].Label);
            Assert.Equal("Portale HR", h.ViewModel.HistoryEntries[0].Label);
            Assert.True(h.ViewModel.HistoryEntries[0].HasLabel);
        }

        [Fact]
        public void ModificaTitolo_AggiungeCambiaETogliereIlTitolo()
        {
            var h = CreateViewModel("a");
            RegisterChange(h.ViewModel, 1);
            Assert.False(h.ViewModel.HistoryEntries[0].HasLabel);

            h.Dialogs.Texts.Enqueue("VPN");
            h.ViewModel.HistoryEntries[0].EditLabelCommand.Execute(null);
            Assert.Equal("VPN", h.History.Load().Entries[0].Label);

            h.Dialogs.Texts.Enqueue("");
            h.ViewModel.HistoryEntries[0].EditLabelCommand.Execute(null);
            Assert.False(h.ViewModel.HistoryEntries[0].HasLabel);
            Assert.True(string.IsNullOrEmpty(h.History.Load().Entries[0].Label));
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
        public void CambioConUnaPasswordGiaNelloStorico_ViengeRifiutato()
        {
            var h = CreateViewModel("a");
            RegisterChange(h.ViewModel, 1);

            RegisterChange(h.ViewModel, 1);   // stessa proposta: coincide con quella appena registrata

            Assert.Single(h.ViewModel.HistoryEntries);
            Assert.Contains("coincide", h.ViewModel.StatusMessage);
        }

        [Fact]
        public void AzzeraStorico_ConConfermaSvuotaLoStorico()
        {
            var h = CreateViewModel("a");
            RegisterChange(h.ViewModel, 1);

            h.ViewModel.ClearHistoryCommand.Execute(null);

            Assert.Empty(h.ViewModel.HistoryEntries);
            Assert.Empty(h.History.Load().Entries);
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


        // ---- blocco: Windows Hello, PIN e password dell'app ----

        [Fact]
        public void AttivaBloccoConPin_SalvaIlPinEAttivaIlBlocco()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 1;                // Windows Hello, PIN, password: si sceglie il PIN
            h.Dialogs.NewSecret = "246810";

            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            Assert.Equal(3, h.Dialogs.ChooseOptions.Count);
            Assert.True(h.ViewModel.LockEnabled);
            Assert.True(h.Settings.Load().LockEnabled);
            Assert.Equal(CredentialKind.Pin, h.Lock.CredentialKind);
            Assert.True(File.Exists(Path.Combine(_directory, "a", "lock.dat")));
        }

        [Fact]
        public void AttivaBloccoConWindowsHello_NonImpostaNessunPin()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 0;

            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            Assert.False(h.ViewModel.HasCredential);
            Assert.Equal(1, h.Hello.Authentications);   // verifica che Windows Hello funzioni, prima di attivare
        }

        [Fact]
        public void SenzaWindowsHello_ILBloccoSiAttivaSoloConPinOPassword()
        {
            var h = CreateViewModel("a", helloAvailable: false);
            h.Dialogs.ChooseIndex = 0;                // senza Hello le voci sono solo PIN e password
            h.Dialogs.NewSecret = "246810";

            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            Assert.Equal(2, h.Dialogs.ChooseOptions.Count);
            Assert.Equal(CredentialKind.Pin, h.Lock.CredentialKind);
            Assert.True(h.Lock.HasCredential);
            Assert.Equal(0, h.Hello.Authentications);
        }

        [Fact]
        public void SbloccoConPin_GiustoSblocca_SbagliatoDaIlConteggioDeiTentativi()
        {
            var h = CreateViewModel("a", helloAvailable: false);
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");
            h.Lock.LockNow();
            Assert.True(h.Lock.IsLocked);

            var wrong = h.Lock.TryUnlockWithSecretAsync("000000").GetAwaiter().GetResult();
            Assert.False(wrong.Success);
            Assert.Contains("Ancora 4 tentativi", wrong.Message);
            Assert.True(h.Lock.IsLocked);

            var right = h.Lock.TryUnlockWithSecretAsync("246810").GetAwaiter().GetResult();
            Assert.True(right.Success);
            Assert.False(h.Lock.IsLocked);
        }

        [Fact]
        public void DopoCinqueErrori_CeUnAttesaCrescenteChePersisteAnchoraConIlPinGiusto()
        {
            var h = CreateViewModel("a", helloAvailable: false);
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");
            h.Lock.LockNow();

            for (var i = 0; i < 5; i++)
            {
                h.Lock.TryUnlockWithSecretAsync("111111").GetAwaiter().GetResult();
            }

            var duringWait = h.Lock.TryUnlockWithSecretAsync("246810").GetAwaiter().GetResult();
            Assert.False(duringWait.Success);
            Assert.Contains("riprova tra", duringWait.Message);
            Assert.True(h.Lock.IsLocked);
            Assert.True(h.Credentials.RetryAfter() > TimeSpan.Zero);
        }

        [Fact]
        public void DisattivaBlocco_ConfermaConHello_ERimuoveIlPin()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 1;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            h.ViewModel.LockEnabled = false;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app disattivato"), "disattivazione del blocco");

            Assert.False(h.ViewModel.HasCredential);
            Assert.False(h.Settings.Load().LockEnabled);
            Assert.False(File.Exists(Path.Combine(_directory, "a", "lock.dat")));
        }

        [Fact]
        public void DisattivaBlocco_ConfermaNonRiuscita_ILBloccoResta()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 1;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");
            h.Hello.AuthenticationSucceeds = false;

            h.ViewModel.LockEnabled = false;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Identità non confermata"), "messaggio di conferma fallita");

            Assert.True(h.ViewModel.LockEnabled);
            Assert.True(h.ViewModel.HasCredential);
        }

        [Fact]
        public void SenzaHello_DisattivareChiedeIlPinDellApp()
        {
            var h = CreateViewModel("a", helloAvailable: false);
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            h.Dialogs.ExistingSecret = "000000";   // sbagliato
            h.ViewModel.LockEnabled = false;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Identità non confermata"), "messaggio di PIN errato");
            Assert.True(h.ViewModel.LockEnabled);
            Assert.True(h.Lock.HasCredential);

            h.Dialogs.ExistingSecret = "246810";   // giusto
            h.ViewModel.LockEnabled = false;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app disattivato"), "disattivazione del blocco");
            Assert.False(h.ViewModel.HasCredential);
        }

        [Fact]
        public void SenzaHello_ILPinNonSiPuoRimuovere()
        {
            var h = CreateViewModel("a", helloAvailable: false);
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            h.ViewModel.RemoveCredentialCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("non si possono rimuovere"), "messaggio sul PIN non rimovibile");

            Assert.True(h.ViewModel.HasCredential);
        }

        [Fact]
        public void CambiaPin_ConfermaConHello_ELoSostituisce()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 1;
            h.Dialogs.NewSecret = "246810";
            h.ViewModel.LockEnabled = true;
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Blocco dell'app attivato"), "attivazione del blocco");

            h.Dialogs.ChooseIndex = 1;   // «Password dell'app»
            h.Dialogs.NewSecret = "nuova password lunga";
            h.ViewModel.SetCredentialCommand.Execute(null);
            WaitFor(() => h.Lock.CredentialKind == CredentialKind.Password, "cambio in password");

            Assert.True(h.Credentials.Check("nuova password lunga").Outcome == CredentialCheck.Correct);
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

            h.Dialogs.ChooseIndex = 1;
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

            h.Dialogs.ChooseIndex = 1;
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
            a.Dialogs.ChooseIndex = 1;
            a.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => a.ViewModel.SyncActive, "attivazione sul primo dispositivo");

            var b = CreateViewModel("b");
            b.Dialogs.SaveFilePath = file;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.Dialogs.ChooseIndex = 1;
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
            a.Dialogs.ChooseIndex = 1;
            a.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => a.ViewModel.SyncActive, "attivazione sul primo dispositivo");

            var b = CreateViewModel("b");
            b.Dialogs.SaveFilePath = file;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.Dialogs.ChooseIndex = 1;
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
            h.Dialogs.ChooseIndex = 1;
            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.SyncActive, "attivazione");

            h.ViewModel.StopSyncCommand.Execute(null);

            Assert.False(h.ViewModel.SyncActive);
            Assert.Null(h.Passphrases.Load());
            Assert.Null(h.Settings.Load().SyncPath);
            Assert.True(File.Exists(file));
        }

        // ---- sincronizzazione su Google Drive ----

        [Fact]
        public void ImpostaSuGoogleDrive_AccedeCreaIlFileEAttivaLaSincronizzazione()
        {
            var h = CreateViewModel("a");
            RegisterChange(h.ViewModel, 1);
            h.Dialogs.ChooseIndex = 0;                  // «Il mio Google Drive»
            h.Dialogs.Passphrases.Enqueue(Phrase);

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.Settings.Load().SyncPath == "google-drive:", "salvataggio delle impostazioni di sincronizzazione");

            Assert.Equal(1, h.Drive.SignIns);
            Assert.True(h.ViewModel.SyncActive);
            Assert.Contains("Google Drive", h.ViewModel.SyncSummary);
            Assert.Single(ExchangeFile.Decrypt(h.Drive.Storage.Data, Phrase).Entries);
            Assert.Equal(Phrase, h.Passphrases.Load());
        }

        [Fact]
        public void GoogleDriveNonConfigurato_LOpzioneDiceChiaramenteCosaManca()
        {
            var h = CreateViewModel("a", drive: new FakeDrive { Configured = false });
            h.Dialogs.ChooseIndex = 0;

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("non è configurato"), "messaggio di Drive non configurato");

            Assert.False(h.ViewModel.SyncActive);
            Assert.Equal(0, h.Drive.SignIns);
        }

        [Fact]
        public void AccessoAGoogleNonRiuscito_NonAttivaNulla_EMostraIlMotivo()
        {
            var h = CreateViewModel("a", drive: new FakeDrive { SignInProblem = "Accesso negato: non hai consentito l'uso di Google Drive." });
            h.Dialogs.ChooseIndex = 0;

            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Accesso negato"), "messaggio di accesso negato");

            Assert.False(h.ViewModel.SyncActive);
            Assert.Null(h.Settings.Load().SyncPath);
            Assert.Null(h.Drive.Storage.Data);
        }

        [Fact]
        public void DuePcSuGoogleDrive_SiScambianoLoStorico()
        {
            var shared = new MemoryStorage();

            var a = CreateViewModel("a", drive: new FakeDrive { Storage = shared });
            RegisterChange(a.ViewModel, 1);
            a.Dialogs.ChooseIndex = 0;
            a.Dialogs.Passphrases.Enqueue(Phrase);
            a.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => a.Settings.Load().SyncPath == "google-drive:", "attivazione sul primo PC");

            var b = CreateViewModel("b", drive: new FakeDrive { Storage = shared });
            b.Dialogs.ChooseIndex = 0;
            b.Dialogs.Passphrases.Enqueue(Phrase);
            b.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => b.Settings.Load().SyncPath == "google-drive:", "attivazione sul secondo PC");

            Assert.Single(b.ViewModel.HistoryEntries);
            Assert.Equal(a.ViewModel.HistoryEntries[0].Password, b.ViewModel.HistoryEntries[0].Password);
            Assert.Contains("già", b.Dialogs.Asked.Single());   // il file su Drive esisteva: frase chiesta una sola volta
        }

        [Fact]
        public void SincronizzaOra_SuDriveSenzaAccesso_RifaLAccesso()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.Passphrases.Enqueue(Phrase);
            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.Settings.Load().SyncPath == "google-drive:", "attivazione");
            h.Drive.SignedIn = false;   // l'accesso è scaduto
            var signIns = h.Drive.SignIns;

            h.ViewModel.SyncNowCommand.Execute(null);
            WaitFor(() => h.Drive.SignIns == signIns + 1, "nuovo accesso");
            WaitFor(() => h.ViewModel.StatusMessage.Contains("Sincronizzato"), "sincronizzazione dopo il nuovo accesso");
        }

        [Fact]
        public void DisattivaSincronizzazioneSuDrive_EsceDaGoogle_ELasciaIlFile()
        {
            var h = CreateViewModel("a");
            h.Dialogs.ChooseIndex = 0;
            h.Dialogs.Passphrases.Enqueue(Phrase);
            h.ViewModel.SetupSyncCommand.Execute(null);
            WaitFor(() => h.Settings.Load().SyncPath == "google-drive:", "attivazione");

            h.ViewModel.StopSyncCommand.Execute(null);

            Assert.False(h.ViewModel.SyncActive);
            Assert.Equal(1, h.Drive.SignOuts);
            Assert.NotNull(h.Drive.Storage.Data);
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
            WaitFor(() => b.Settings.Load().LastChangeDate == Today, "salvataggio della data");   // le impostazioni si salvano dopo lo storico

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
