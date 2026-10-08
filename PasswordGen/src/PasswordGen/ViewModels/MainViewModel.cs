using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Security;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using PasswordGen.Mvvm;
using PasswordGen.Services;

namespace PasswordGen.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly PasswordGenerator _generator;
        private readonly WordList _builtinWords;
        private readonly SettingsStore _store;
        private readonly ISecretClipboard _clipboard;
        private readonly IStartupRegistration _startup;
        private readonly Func<DateTime> _today;
        private readonly AppSettings _settings;
        private readonly HistoryStore _historyStore;
        private readonly IDialogService _dialogs;
        private readonly PasswordHistory _history;
        private readonly AppLockController _lock;
        private readonly SyncPassphraseStore _syncPassphrases;
        private bool _syncBusy;

        private bool _loading = true;
        private GenerationMode _mode;
        private int _wordCount;
        private int _syllableCount;
        private int _randomLength;
        private int _suggestionCount;
        private int _minLength;
        private bool _requireUpper;
        private bool _requireLower;
        private bool _requireDigit;
        private bool _requireSpecial;
        private bool _avoidAmbiguous;
        private string _previousPassword = string.Empty;
        private WordSourceMode _wordSource;
        private string _customWordsPath;
        private WordFileResult _customWords;
        private string _wordSummary = string.Empty;
        private string _wordWarning = string.Empty;
        private bool _lockEnabled;
        private int _lockGraceSeconds;
        private bool _historyEnabled;
        private bool _isChoosing;
        private int _choiceIndex;
        private SuggestionViewModel _lastCopied;
        private bool _reminderEnabled;
        private int _validityDays;
        private bool _startWithWindows;
        private string _statusMessage = string.Empty;
        private string _reminderMessage = string.Empty;
        private string _reminderLevel = "Info";
        private string _lastChangeText = string.Empty;

        public MainViewModel(
            PasswordGenerator generator,
            WordList builtinWords,
            SettingsStore store,
            ISecretClipboard clipboard,
            IStartupRegistration startup,
            HistoryStore historyStore,
            IDialogService dialogs,
            AppLockController appLock,
            SyncPassphraseStore syncPassphrases,
            Func<DateTime> today)
        {
            _syncPassphrases = syncPassphrases;
            _generator = generator;
            _builtinWords = builtinWords;
            _lock = appLock;
            _store = store;
            _clipboard = clipboard;
            _startup = startup;
            _historyStore = historyStore;
            _dialogs = dialogs;
            _today = today;

            _settings = store.Load();
            _mode = _settings.Mode;
            _wordCount = _settings.WordCount;
            _syllableCount = _settings.SyllableCount;
            _randomLength = _settings.RandomLength;
            _suggestionCount = _settings.SuggestionCount;
            _minLength = _settings.MinLength;
            _requireUpper = _settings.RequireUpper;
            _requireLower = _settings.RequireLower;
            _requireDigit = _settings.RequireDigit;
            _requireSpecial = _settings.RequireSpecial;
            _avoidAmbiguous = _settings.AvoidAmbiguous;
            _wordSource = _settings.WordSource;
            _customWordsPath = _settings.CustomWordsPath;
            if (!string.IsNullOrEmpty(_customWordsPath))
            {
                _customWords = CustomWordList.Load(_customWordsPath);
            }

            _lockEnabled = _settings.LockEnabled;
            _lockGraceSeconds = _settings.LockGraceSeconds;
            appLock.DisabledAutomatically += (sender, message) =>
            {
                _lockEnabled = false;
                OnPropertyChanged(nameof(LockEnabled));
                OnPropertyChanged(nameof(CanLockNow));
                SaveSettings();
                StatusMessage = message;
            };
            _historyEnabled = _settings.HistoryEnabled;
            _history = _historyEnabled ? historyStore.Load() : new PasswordHistory();
            _reminderEnabled = _settings.ReminderEnabled;
            _validityDays = _settings.ValidityDays;

            try
            {
                _startWithWindows = startup.IsEnabled;
            }
            catch (Exception)
            {
                _startWithWindows = false;
            }

            Suggestions = new ObservableCollection<SuggestionViewModel>();
            HistoryEntries = new ObservableCollection<HistoryEntryViewModel>();
            ChoiceItems = new ObservableCollection<string>();
            GenerateCommand = new RelayCommand(Generate);
            MarkChangedCommand = new RelayCommand(MarkChanged);
            ConfirmChangeCommand = new RelayCommand(ConfirmChange);
            LoadWordFileCommand = new RelayCommand(LoadWordFile);
            ResetWordsCommand = new RelayCommand(ResetWords, () => _customWords != null || _wordSource != WordSourceMode.Builtin);
            CancelChangeCommand = new RelayCommand(() => IsChoosing = false);
            ClearHistoryCommand = new RelayCommand(ClearHistory, () => _history.Entries.Count > 0);
            SetupSyncCommand = new RelayCommand(() => { var ignored = SetupSyncAsync(); }, () => !_syncBusy);
            SyncNowCommand = new RelayCommand(() => { var ignored = SyncNowAsync(); }, () => SyncActive && !_syncBusy);
            StopSyncCommand = new RelayCommand(StopSync, () => SyncActive && !_syncBusy);
            ExportCommand = new RelayCommand(() => { var ignored = ExportAsync(); }, () => !_syncBusy);
            ImportCommand = new RelayCommand(() => { var ignored = ImportAsync(); }, () => !_syncBusy);
            SetCredentialCommand = new RelayCommand(() => { var ignored = SetCredentialAsync(); }, () => CanSetCredential);
            RemoveCredentialCommand = new RelayCommand(() => { var ignored = RemoveCredentialAsync(); }, () => _lock.HasCredential);
            DismissLockHintCommand = new RelayCommand(DismissLockHint);
            EnableLockFromHintCommand = new RelayCommand(() => { DismissLockHint(); LockEnabled = true; });

            _clipboard.Cleared += (s, e) => StatusMessage = "Appunti svuotati.";

            _loading = false;
            RefreshReminder();
            RefreshHistory();
            ApplyWordSource();
            Generate();
        }

        // ------------------------------------------------------------ Generale

        public string WindowTitle
        {
            get { return AppInfo.Name + " " + AppInfo.Version; }
        }

        public string AppVersion
        {
            get { return AppInfo.Version; }
        }

        public ObservableCollection<SuggestionViewModel> Suggestions { get; private set; }

        public ICommand GenerateCommand { get; private set; }

        public ICommand MarkChangedCommand { get; private set; }

        public ICommand ConfirmChangeCommand { get; private set; }

        public ICommand LoadWordFileCommand { get; private set; }

        public ICommand ResetWordsCommand { get; private set; }

        public ICommand CancelChangeCommand { get; private set; }

        public ICommand ClearHistoryCommand { get; private set; }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty(ref _statusMessage, value); }
        }

        // ------------------------------------------------------------ Tipo di password

        public bool IsPassphraseMode
        {
            get { return _mode == GenerationMode.Passphrase; }
            set { if (value) { SetMode(GenerationMode.Passphrase); } }
        }

        public bool IsSyllablesMode
        {
            get { return _mode == GenerationMode.Syllables; }
            set { if (value) { SetMode(GenerationMode.Syllables); } }
        }

        public bool IsRandomMode
        {
            get { return _mode == GenerationMode.Random; }
            set { if (value) { SetMode(GenerationMode.Random); } }
        }

        private void SetMode(GenerationMode mode)
        {
            if (_mode == mode)
            {
                return;
            }

            _mode = mode;
            OnPropertyChanged(nameof(IsPassphraseMode));
            OnPropertyChanged(nameof(IsSyllablesMode));
            OnPropertyChanged(nameof(IsRandomMode));
            Generate();
        }

        public int WordCount
        {
            get { return _wordCount; }
            set { SetOption(ref _wordCount, value); }
        }

        public int SyllableCount
        {
            get { return _syllableCount; }
            set { SetOption(ref _syllableCount, value); }
        }

        public int RandomLength
        {
            get { return _randomLength; }
            set { SetOption(ref _randomLength, value); }
        }

        /// <summary>Quante proposte generare per volta (1-20).</summary>
        public int SuggestionCount
        {
            get { return _suggestionCount; }
            set { SetOption(ref _suggestionCount, value); }
        }

        // ------------------------------------------------------------ Parole della passphrase

        public bool IsBuiltinWords
        {
            get { return _wordSource == WordSourceMode.Builtin; }
            set { if (value) { SetWordSource(WordSourceMode.Builtin); } }
        }

        public bool IsCombinedWords
        {
            get { return _wordSource == WordSourceMode.Combined; }
            set { if (value) { SetWordSource(WordSourceMode.Combined); } }
        }

        public bool IsCustomOnlyWords
        {
            get { return _wordSource == WordSourceMode.CustomOnly; }
            set { if (value) { SetWordSource(WordSourceMode.CustomOnly); } }
        }

        /// <summary>True se è stato caricato un file leggibile con almeno una parola valida.</summary>
        public bool HasCustomWords
        {
            get { return _customWords != null && _customWords.Error == null && _customWords.Words.Count > 0; }
        }

        /// <summary>Lista in uso e quanto vale ogni parola in bit.</summary>
        public string WordSummary
        {
            get { return _wordSummary; }
            private set { SetProperty(ref _wordSummary, value); }
        }

        public string CustomFileSummary
        {
            get
            {
                return _customWords == null
                    ? "Nessun file caricato."
                    : _customWords.FileName + ": " + _customWords.Summary;
            }
        }

        public string WordWarning
        {
            get { return _wordWarning; }
            private set
            {
                if (SetProperty(ref _wordWarning, value))
                {
                    OnPropertyChanged(nameof(HasWordWarning));
                }
            }
        }

        public bool HasWordWarning
        {
            get { return _wordWarning.Length > 0; }
        }

        private void SetWordSource(WordSourceMode mode)
        {
            if (_wordSource == mode)
            {
                return;
            }

            _wordSource = mode;
            ApplyWordSource();
            Generate();
        }

        private void LoadWordFile()
        {
            var path = _dialogs.PickFile("Scegli il file delle parole", "File di testo (*.txt)|*.txt|Tutti i file (*.*)|*.*");
            if (path == null)
            {
                return;
            }

            var result = CustomWordList.Load(path);
            if (result.Error != null || result.Words.Count == 0)
            {
                StatusMessage = result.Error ?? "Il file non contiene parole valide (una per riga, 4-9 lettere): " + result.Summary;
                return;
            }

            _customWords = result;
            _customWordsPath = path;
            if (_wordSource == WordSourceMode.Builtin)
            {
                _wordSource = WordSourceMode.Combined;
            }

            ApplyWordSource();
            Generate();
            SaveSettings();
            StatusMessage = "Caricate " + result.Words.Count + " parole da " + result.FileName + ".";
        }

        private void ResetWords()
        {
            _customWords = null;
            _customWordsPath = null;
            _wordSource = WordSourceMode.Builtin;
            ApplyWordSource();
            Generate();
            SaveSettings();
            StatusMessage = "Ripristinata la lista di parole integrata.";
        }

        /// <summary>Sceglie la lista di parole in base alla modalità e al file, e aggiorna riepilogo e avvisi.</summary>
        private void ApplyWordSource()
        {
            var selection = WordSelection.Select(_builtinWords, _customWords, _wordSource);
            _generator.SetWords(selection.List);

            string origin;
            switch (selection.Effective)
            {
                case WordSourceMode.Combined:
                    origin = "Lista integrata + " + _customWords.FileName;
                    break;
                case WordSourceMode.CustomOnly:
                    origin = "Solo " + _customWords.FileName;
                    break;
                default:
                    origin = "Lista integrata";
                    break;
            }

            WordSummary = origin + ": " + selection.List.Count + " parole, "
                + selection.BitsPerWord.ToString("0.0", CultureInfo.CurrentCulture) + " bit per parola";
            WordWarning = selection.Warning;

            OnPropertyChanged(nameof(IsBuiltinWords));
            OnPropertyChanged(nameof(IsCombinedWords));
            OnPropertyChanged(nameof(IsCustomOnlyWords));
            OnPropertyChanged(nameof(HasCustomWords));
            OnPropertyChanged(nameof(CustomFileSummary));
        }

        // ------------------------------------------------------------ Policy

        public int MinLength
        {
            get { return _minLength; }
            set
            {
                if (SetOption(ref _minLength, value))
                {
                    OnPropertyChanged(nameof(PolicySummary));
                }
            }
        }

        public bool RequireUpper
        {
            get { return _requireUpper; }
            set { SetPolicyFlag(ref _requireUpper, value); }
        }

        public bool RequireLower
        {
            get { return _requireLower; }
            set { SetPolicyFlag(ref _requireLower, value); }
        }

        public bool RequireDigit
        {
            get { return _requireDigit; }
            set { SetPolicyFlag(ref _requireDigit, value); }
        }

        public bool RequireSpecial
        {
            get { return _requireSpecial; }
            set { SetPolicyFlag(ref _requireSpecial, value); }
        }

        public bool AvoidAmbiguous
        {
            get { return _avoidAmbiguous; }
            set { SetOption(ref _avoidAmbiguous, value); }
        }

        /// <summary>Riepilogo leggibile delle regole attive.</summary>
        public string PolicySummary
        {
            get
            {
                var parts = new System.Collections.Generic.List<string> { "almeno " + _minLength + " caratteri" };
                if (_requireUpper) { parts.Add("maiuscole"); }
                if (_requireLower) { parts.Add("minuscole"); }
                if (_requireDigit) { parts.Add("numeri"); }
                if (_requireSpecial) { parts.Add("caratteri speciali"); }
                return string.Join(", ", parts);
            }
        }

        private void SetPolicyFlag(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (SetOption(ref field, value, name))
            {
                OnPropertyChanged(nameof(PolicySummary));
            }
        }

        /// <summary>Password attuale (facoltativa): resta in memoria, non viene salvata. Le nuove proposte la evitano.</summary>
        public string PreviousPassword
        {
            get { return _previousPassword; }
            set { SetProperty(ref _previousPassword, value ?? string.Empty); }
        }

        private bool SetOption<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (!SetProperty(ref field, value, name))
            {
                return false;
            }

            if (!_loading)
            {
                Generate();
            }

            return true;
        }

        // ------------------------------------------------------------ Promemoria

        public bool ReminderEnabled
        {
            get { return _reminderEnabled; }
            set
            {
                if (SetProperty(ref _reminderEnabled, value))
                {
                    RefreshReminder();
                    SaveSettings();
                }
            }
        }

        public int ValidityDays
        {
            get { return _validityDays; }
            set
            {
                if (SetProperty(ref _validityDays, value))
                {
                    RefreshReminder();
                }
            }
        }

        public bool StartWithWindows
        {
            get { return _startWithWindows; }
            set
            {
                if (value == _startWithWindows)
                {
                    return;
                }

                try
                {
                    _startup.SetEnabled(value);
                    SetProperty(ref _startWithWindows, value);
                    StatusMessage = value
                        ? "Il promemoria si aprirà all'accesso a Windows solo quando la password è in scadenza."
                        : "Avvio automatico disattivato.";
                }
                catch (Exception ex)
                {
                    StatusMessage = "Impossibile modificare l'avvio automatico: " + ex.Message;
                    RefreshLater(nameof(StartWithWindows));
                }
            }
        }

        public string ReminderMessage
        {
            get { return _reminderMessage; }
            private set { SetProperty(ref _reminderMessage, value); }
        }

        /// <summary>Info, Ok, Warn o Error: il tema colora il banner di conseguenza.</summary>
        public string ReminderLevel
        {
            get { return _reminderLevel; }
            private set { SetProperty(ref _reminderLevel, value); }
        }

        public string LastChangeText
        {
            get { return _lastChangeText; }
            private set { SetProperty(ref _lastChangeText, value); }
        }

        private void RefreshReminder()
        {
            var last = _settings.LastChangeDate;
            var state = ChangeReminder.Evaluate(_reminderEnabled, last, _validityDays, _settings.WarnDays, _today());

            ReminderMessage = state.Message;
            switch (state.Status)
            {
                case ReminderStatus.Expired:
                    ReminderLevel = "Error";
                    break;
                case ReminderStatus.DueSoon:
                    ReminderLevel = "Warn";
                    break;
                case ReminderStatus.Ok:
                    ReminderLevel = "Ok";
                    break;
                default:
                    ReminderLevel = "Info";
                    break;
            }

            LastChangeText = last.HasValue
                ? "Ultimo cambio: " + last.Value.ToString("d", CultureInfo.CurrentCulture)
                : "Nessun cambio registrato";
        }

        /// <summary>
        /// Con lo storico attivo chiede quale proposta è stata usata (preselezionata l'ultima copiata);
        /// altrimenti registra direttamente la data.
        /// </summary>
        private void MarkChanged()
        {
            if (_historyEnabled && Suggestions.Count > 0)
            {
                ChoiceItems.Clear();
                ChoiceItems.Add("Nessuna: registra solo la data del cambio");
                for (var i = 0; i < Suggestions.Count; i++)
                {
                    ChoiceItems.Add("Proposta " + (i + 1) + "  -  " + Suggestions[i].Text);
                }

                ChoiceIndex = _lastCopied == null ? 0 : Suggestions.IndexOf(_lastCopied) + 1;
                IsChoosing = true;
                return;
            }

            CompleteChange(null);
        }

        private void ConfirmChange()
        {
            SuggestionViewModel chosen = null;
            if (_choiceIndex > 0 && _choiceIndex <= Suggestions.Count)
            {
                chosen = Suggestions[_choiceIndex - 1];
            }

            IsChoosing = false;
            CompleteChange(chosen);
        }

        private void CompleteChange(SuggestionViewModel chosen)
        {
            var today = _today();
            _settings.LastChangeDate = today;

            if (_historyEnabled)
            {
                var entry = _history.Add(chosen == null ? null : chosen.Text, chosen == null ? _mode : chosen.Mode, today);
                SaveHistory();
                RefreshHistory();
                StatusMessage = chosen == null
                    ? "Cambio registrato (#" + entry.Number + ", solo data): " + ReminderTextAfterRefresh()
                    : "Cambio registrato nello storico come #" + entry.Number + ": " + ReminderTextAfterRefresh();
            }
            else
            {
                StatusMessage = "Cambio password registrato: " + ReminderTextAfterRefresh();
            }

            SaveSettings();
            var ignoredSync = AutoSyncAsync();
        }

        private string ReminderTextAfterRefresh()
        {
            RefreshReminder();
            return ReminderMessage;
        }

        // ------------------------------------------------------------ Sincronizzazione e backup

        private const string SyncFilter = "File di PasswordGen (*.pgx)|*.pgx|Tutti i file (*.*)|*.*";

        public ICommand SetupSyncCommand { get; private set; }

        public ICommand SyncNowCommand { get; private set; }

        public ICommand StopSyncCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        public bool SyncActive
        {
            get { return !string.IsNullOrEmpty(_settings.SyncPath); }
        }

        public string SyncSummary
        {
            get
            {
                if (!SyncActive)
                {
                    return "Sincronizzazione non attiva.";
                }

                var last = ExchangeData.ParseTime(_settings.LastSyncUtcText);
                return "Attiva con il file " + _settings.SyncPath + ". "
                    + (last.HasValue ? "Ultima sincronizzazione: " + last.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) + "." : "Non ancora sincronizzato.");
            }
        }

        private void RefreshSyncState()
        {
            OnPropertyChanged(nameof(SyncActive));
            OnPropertyChanged(nameof(SyncSummary));
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task SetupSyncAsync()
        {
            if (!_historyEnabled)
            {
                StatusMessage = "Per sincronizzare attiva prima lo storico delle password.";
                return;
            }

            var path = _dialogs.PickSaveFile("Scegli o crea il file di sincronizzazione (per esempio nella cartella di Google Drive)",
                SyncFilter, "PasswordGen-sync.pgx", false);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var exists = File.Exists(path) && new FileInfo(path).Length > 0;
            var passphrase = _dialogs.AskPassphrase("Sincronizzazione",
                exists
                    ? "Il file esiste già. Inserisci la frase segreta con cui è stato creato."
                    : "Scegli una frase segreta per cifrare il file. Ti servirà anche sull'altro dispositivo e non si può recuperare.",
                !exists);
            if (passphrase == null)
            {
                return;
            }

            if (await RunSyncAsync(path, passphrase))
            {
                // Prima la frase, poi il percorso: appena la sincronizzazione risulta attiva, la frase c'è già.
                try
                {
                    _syncPassphrases.Save(passphrase);
                }
                catch (Exception ex)
                {
                    StatusMessage = "Impossibile salvare la frase segreta: " + ex.Message;
                }

                _settings.SyncPath = path;
                SaveSettings();
                RefreshSyncState();
            }
        }

        private async Task SyncNowAsync()
        {
            var passphrase = _syncPassphrases.Load()
                ?? _dialogs.AskPassphrase("Sincronizzazione", "Inserisci la frase segreta del file di sincronizzazione.", false);
            if (passphrase == null)
            {
                return;
            }

            if (await RunSyncAsync(_settings.SyncPath, passphrase))
            {
                try
                {
                    _syncPassphrases.Save(passphrase);
                }
                catch (Exception)
                {
                    // La frase verrà richiesta di nuovo la prossima volta.
                }
            }
        }

        /// <summary>Sincronizza in silenzio (all'avvio e dopo un cambio): niente finestre, solo un messaggio nella barra di stato.</summary>
        public async Task AutoSyncAsync()
        {
            if (!SyncActive || _syncBusy || !_historyEnabled)
            {
                return;
            }

            var passphrase = _syncPassphrases.Load();
            if (passphrase != null)
            {
                await RunSyncAsync(_settings.SyncPath, passphrase);
            }
        }

        private async Task<bool> RunSyncAsync(string path, string passphrase)
        {
            _syncBusy = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                SaveSettings();   // porta nelle impostazioni i valori correnti (per esempio la durata della password)
                var result = await SyncEngine.RunAsync(new FileSyncStorage(path), passphrase, _history, _settings, DateTime.UtcNow);
                if (!result.Succeeded)
                {
                    StatusMessage = result.Message;
                    return false;
                }

                _validityDays = _settings.ValidityDays;
                OnPropertyChanged(nameof(ValidityDays));
                SaveHistory();
                RefreshHistory();
                RefreshReminder();
                SaveSettings();
                StatusMessage = result.EntriesAdded > 0
                    ? "Sincronizzato: " + result.EntriesAdded + (result.EntriesAdded == 1 ? " voce nuova" : " voci nuove") + " dall'altro dispositivo."
                    : "Sincronizzato: tutto era già aggiornato.";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Sincronizzazione non riuscita: " + ex.Message;
                return false;
            }
            finally
            {
                _syncBusy = false;
                RefreshSyncState();
            }
        }

        private void StopSync()
        {
            if (!_dialogs.Confirm("Disattivare la sincronizzazione? Il file resta dov'è e i dati su questo PC non cambiano.", "Sincronizzazione"))
            {
                return;
            }

            _settings.SyncPath = null;
            _settings.LastSyncUtcText = null;
            try
            {
                _syncPassphrases.Delete();
            }
            catch (Exception)
            {
                // Il file della frase sarà sovrascritto alla prossima attivazione.
            }

            SaveSettings();
            RefreshSyncState();
            StatusMessage = "Sincronizzazione disattivata.";
        }

        private async Task ExportAsync()
        {
            var passphrase = _dialogs.AskPassphrase("Esporta lo storico",
                "Scegli la frase segreta che cifra il file: servirà per importarlo. Non si può recuperare.", true);
            if (passphrase == null)
            {
                return;
            }

            var path = _dialogs.PickSaveFile("Salva il file esportato", SyncFilter, "PasswordGen-backup.pgx", true);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _syncBusy = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                SaveSettings();
                var history = _history.Clone();
                var settings = _settings.Clone();
                var bytes = await Task.Run(() => ExchangeFile.Export(history, settings, DateTime.UtcNow, passphrase));
                File.WriteAllBytes(path, bytes);
                StatusMessage = "Esportati " + history.Entries.Count + " cambi password in " + path + ".";
            }
            catch (Exception ex)
            {
                StatusMessage = "Esportazione non riuscita: " + ex.Message;
            }
            finally
            {
                _syncBusy = false;
                RefreshSyncState();
            }
        }

        private async Task ImportAsync()
        {
            if (!_historyEnabled)
            {
                StatusMessage = "Per importare attiva prima lo storico delle password.";
                return;
            }

            var path = _dialogs.PickFile("Scegli il file da importare", SyncFilter);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var passphrase = _dialogs.AskPassphrase("Importa lo storico", "Inserisci la frase segreta del file.", false);
            if (passphrase == null)
            {
                return;
            }

            _syncBusy = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                var bytes = File.ReadAllBytes(path);
                var data = await Task.Run(() => ExchangeFile.Decrypt(bytes, passphrase));
                var summary = ExchangeFile.Merge(data, _history, _settings, true);
                _validityDays = _settings.ValidityDays;
                OnPropertyChanged(nameof(ValidityDays));
                SaveHistory();
                RefreshHistory();
                RefreshReminder();
                SaveSettings();
                StatusMessage = "Importate " + summary.EntriesAdded + (summary.EntriesAdded == 1 ? " voce nuova" : " voci nuove") + " nello storico.";
            }
            catch (CryptographicException)
            {
                StatusMessage = "La frase segreta non è quella del file (o il file è stato alterato).";
            }
            catch (InvalidDataException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = "Importazione non riuscita: " + ex.Message;
            }
            finally
            {
                _syncBusy = false;
                RefreshSyncState();
            }
        }

        // ------------------------------------------------------------ Suggerimento per il blocco

        public ICommand DismissLockHintCommand { get; private set; }

        public ICommand EnableLockFromHintCommand { get; private set; }

        /// <summary>Invita ad attivare il blocco finché non è attivo o l'utente chiude l'avviso.</summary>
        public bool ShowLockHint
        {
            get { return !_lockEnabled && !_settings.LockHintDismissed; }
        }

        private void DismissLockHint()
        {
            _settings.LockHintDismissed = true;
            OnPropertyChanged(nameof(ShowLockHint));
            SaveSettings();
        }

        // ------------------------------------------------------------ Blocco dell'app

        private static readonly int[] GraceSeconds = { 0, 30, 60, 300 };

        public string[] LockGraceNames
        {
            get { return new[] { "Subito", "Dopo 30 secondi", "Dopo 1 minuto", "Dopo 5 minuti" }; }
        }

        public AppLockController Lock
        {
            get { return _lock; }
        }

        /// <summary>Richiede Windows Hello (PIN, impronta o volto) per aprire l'app.</summary>
        public bool LockEnabled
        {
            get { return _lockEnabled; }
            set
            {
                if (value == _lockEnabled)
                {
                    return;
                }

                // La casella resta com'era finché Windows Hello non ha confermato.
                RefreshLater(nameof(LockEnabled));
                var ignored = ChangeLockAsync(value);
            }
        }

        public int LockGraceIndex
        {
            get
            {
                var index = Array.IndexOf(GraceSeconds, _lockGraceSeconds);
                return index >= 0 ? index : 1;
            }
            set
            {
                if (value < 0 || value >= GraceSeconds.Length || GraceSeconds[value] == _lockGraceSeconds)
                {
                    return;
                }

                _lockGraceSeconds = GraceSeconds[value];
                _lock.State.GracePeriod = TimeSpan.FromSeconds(_lockGraceSeconds);
                OnPropertyChanged();
                SaveSettings();
            }
        }

        /// <summary>Il pulsante «Blocca adesso» ha senso solo con il blocco attivo.</summary>
        public bool CanLockNow
        {
            get { return _lockEnabled; }
        }

        public ICommand LockNowCommand
        {
            get { return new RelayCommand(_lock.LockNow, () => _lockEnabled); }
        }

        /// <summary>
        /// Conferma l'identità prima di cambiare il blocco: Windows Hello se c'è; altrimenti il PIN o la password dell'app.
        /// Senza né l'uno né l'altro (blocco non ancora attivo) non c'è nulla da confermare.
        /// </summary>
        private async Task<bool> ConfirmIdentityAsync(string message)
        {
            if (_lock.HelloAvailable)
            {
                var outcome = await _lock.ConfirmAsync(message);
                if (!outcome.Success)
                {
                    StatusMessage = "Verifica non riuscita (" + outcome.Reason + ").";
                    return false;
                }

                return true;
            }

            if (_lock.HasCredential)
            {
                var secret = _dialogs.AskSecret("Conferma", "Inserisci il " + (_lock.CredentialKind == CredentialKind.Pin ? "PIN" : "la password") + " dell'app per confermare.");
                if (secret == null)
                {
                    StatusMessage = "Identità non confermata.";
                    return false;
                }

                var attempt = await _lock.VerifySecretAsync(secret);
                if (!attempt.Success)
                {
                    StatusMessage = attempt.Message;
                    return false;
                }
            }

            return true;
        }

        private async Task ChangeLockAsync(bool enable)
        {
            try
            {
                if (!enable)
                {
                    // Chi trova il PC sbloccato non può togliere il blocco: serve confermare l'identità.
                    if (!await ConfirmIdentityAsync("Conferma per disattivare il blocco di PasswordGen"))
                    {
                        StatusMessage = "Identità non confermata: il blocco è rimasto com'era.";
                        return;
                    }

                    _lockEnabled = false;
                    _lock.SetEnabled(false);
                    _lock.ClearCredential();
                    RefreshLockProperties();
                    SaveSettings();
                    StatusMessage = "Blocco dell'app disattivato (l'eventuale PIN o password dell'app sono stati rimossi).";
                    return;
                }

                await _lock.RefreshAvailabilityAsync();
                var hello = _lock.HelloAvailable;
                var options = new System.Collections.Generic.List<string>();
                var kinds = new System.Collections.Generic.List<CredentialKind?>();
                if (hello)
                {
                    options.Add("Windows Hello (PIN, impronta o volto)");
                    kinds.Add(null);
                }

                if (_lock.CredentialsSupported)
                {
                    options.Add("PIN dell'app (4-12 cifre)");
                    kinds.Add(CredentialKind.Pin);
                    options.Add("Password dell'app");
                    kinds.Add(CredentialKind.Password);
                }

                if (options.Count == 0)
                {
                    StatusMessage = "Per usare il blocco serve Windows Hello (Impostazioni, Account, Opzioni di accesso) oppure un PIN o una password dell'app.";
                    return;
                }

                var index = options.Count == 1 ? 0 : _dialogs.Choose("Come vuoi sbloccare PasswordGen?", options);
                if (index < 0)
                {
                    return;
                }

                var kind = kinds[index];
                if (kind.HasValue)
                {
                    var secret = _dialogs.AskNewSecret(kind.Value);
                    if (secret == null)
                    {
                        StatusMessage = "Nessun PIN o password impostati: il blocco è rimasto com'era.";
                        return;
                    }

                    await _lock.SetCredentialAsync(kind.Value, secret);
                }
                else
                {
                    // Con Windows Hello si verifica subito che funzioni, prima di attivare il blocco.
                    var outcome = await _lock.ConfirmAsync("Conferma per attivare il blocco di PasswordGen");
                    if (!outcome.Success)
                    {
                        StatusMessage = "Verifica non riuscita (" + outcome.Reason + "): il blocco è rimasto com'era.";
                        return;
                    }
                }

                _lockEnabled = true;
                _lock.SetEnabled(true);
                RefreshLockProperties();
                SaveSettings();
                StatusMessage = "Blocco dell'app attivato: si attiva alla prossima apertura e " + LockGraceNames[LockGraceIndex].ToLowerInvariant() + " in secondo piano. Prova «Blocca adesso».";
            }
            catch (Exception ex)
            {
                StatusMessage = "Errore del blocco: " + ex.Message;
            }
        }

        // ---------------------------------------------------------------- PIN o password dell'app

        public ICommand SetCredentialCommand { get; private set; }

        public ICommand RemoveCredentialCommand { get; private set; }

        public bool HasCredential
        {
            get { return _lock.HasCredential; }
        }

        /// <summary>Si può cambiare il PIN o la password solo con il blocco attivo.</summary>
        public bool CanSetCredential
        {
            get { return _lockEnabled && _lock.CredentialsSupported; }
        }

        public string CredentialText
        {
            get
            {
                if (!_lock.HasCredential)
                {
                    return "Nessun PIN o password dell'app: si sblocca con Windows Hello.";
                }

                return _lock.CredentialKind == CredentialKind.Pin
                    ? "PIN dell'app impostato (si può sbloccare anche con Windows Hello, se configurato)."
                    : "Password dell'app impostata (si può sbloccare anche con Windows Hello, se configurato).";
            }
        }

        /// <summary>Imposta o cambia il PIN o la password dell'app (con il blocco già attivo).</summary>
        private async Task SetCredentialAsync()
        {
            if (!_lockEnabled || !_lock.CredentialsSupported)
            {
                return;
            }

            try
            {
                if (!await ConfirmIdentityAsync("Conferma per cambiare il PIN o la password di PasswordGen"))
                {
                    StatusMessage = "Identità non confermata: nulla è cambiato.";
                    return;
                }

                var index = _dialogs.Choose("Che cosa vuoi impostare?", new[] { "PIN dell'app (4-12 cifre)", "Password dell'app" });
                if (index < 0)
                {
                    return;
                }

                var kind = index == 0 ? CredentialKind.Pin : CredentialKind.Password;
                var secret = _dialogs.AskNewSecret(kind);
                if (secret == null)
                {
                    return;
                }

                await _lock.SetCredentialAsync(kind, secret);
                _lock.RefreshCredentialProperties();
                RefreshLockProperties();
                StatusMessage = kind == CredentialKind.Pin ? "PIN dell'app impostato." : "Password dell'app impostata.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Errore: " + ex.Message;
            }
        }

        private async Task RemoveCredentialAsync()
        {
            if (!_lock.HasCredential)
            {
                return;
            }

            // Senza Windows Hello il PIN o la password dell'app sono l'unico modo per sbloccare: non si possono togliere.
            if (!_lock.HelloAvailable)
            {
                StatusMessage = "Windows Hello non è configurato: il PIN o la password dell'app sono l'unico modo di sbloccare e non si possono rimuovere (puoi cambiarli o disattivare il blocco).";
                return;
            }

            if (!await ConfirmIdentityAsync("Conferma per rimuovere il PIN o la password di PasswordGen"))
            {
                StatusMessage = "Identità non confermata: nulla è cambiato.";
                return;
            }

            _lock.ClearCredential();
            RefreshLockProperties();
            StatusMessage = "PIN o password dell'app rimossi: si sblocca con Windows Hello.";
        }

        private void RefreshLockProperties()
        {
            OnPropertyChanged(nameof(LockEnabled));
            OnPropertyChanged(nameof(CanLockNow));
            OnPropertyChanged(nameof(CanSetCredential));
            OnPropertyChanged(nameof(HasCredential));
            OnPropertyChanged(nameof(CredentialText));
            OnPropertyChanged(nameof(ShowLockHint));
        }

        /// <summary>Da chiamare quando l'app si blocca: via la password attuale e password dello storico di nuovo mascherate.</summary>
        public void ClearSensitive()
        {
            PreviousPassword = string.Empty;
            foreach (var entry in HistoryEntries)
            {
                entry.Hide();
            }
        }

        /// <summary>
        /// WPF ignora una notifica di modifica sollevata mentre sta aggiornando la sorgente: per riportare una casella allo stato
        /// precedente la notifica va rimandata di un istante.
        /// </summary>
        private void RefreshLater(string propertyName)
        {
            var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(new Action(() => OnPropertyChanged(propertyName)));
            }
            else
            {
                OnPropertyChanged(propertyName);
            }
        }

        // ------------------------------------------------------------ Storico

        /// <summary>Conserva le password scelte in un file cifrato per il tuo utente Windows. Disattivandolo, lo storico viene cancellato.</summary>
        public bool HistoryEnabled
        {
            get { return _historyEnabled; }
            set
            {
                if (value == _historyEnabled)
                {
                    return;
                }

                if (!value && _history.Entries.Count > 0
                    && !_dialogs.Confirm("Disattivando lo storico, le password conservate vengono cancellate. Continuare?", "Storico"))
                {
                    RefreshLater(nameof(HistoryEnabled));
                    return;
                }

                _historyEnabled = value;
                OnPropertyChanged();
                if (!value)
                {
                    ClearHistoryEntries();
                }

                SaveSettings();
            }
        }

        public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; private set; }

        public bool HasHistory
        {
            get { return _history.Entries.Count > 0; }
        }

        public string HistoryHeader
        {
            get { return _history.Entries.Count > 0 ? "Storico (" + _history.Entries.Count + ")" : "Storico"; }
        }

        /// <summary>True mentre l'app chiede quale proposta è stata usata come nuova password.</summary>
        public bool IsChoosing
        {
            get { return _isChoosing; }
            private set { SetProperty(ref _isChoosing, value); }
        }

        public ObservableCollection<string> ChoiceItems { get; private set; }

        /// <summary>0 = nessuna proposta (solo data); n = proposta numero n.</summary>
        public int ChoiceIndex
        {
            get { return _choiceIndex; }
            set { SetProperty(ref _choiceIndex, value); }
        }

        private void RefreshHistory()
        {
            HistoryEntries.Clear();
            foreach (var entry in _history.Entries)
            {
                HistoryEntries.Add(new HistoryEntryViewModel(entry, CopyHistoryEntry, DeleteHistoryEntry));
            }

            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(HistoryHeader));
        }

        private void CopyHistoryEntry(HistoryEntryViewModel entry)
        {
            StatusMessage = _clipboard.Copy(entry.Password)
                ? "Password #" + entry.Number + " copiata: verrà cancellata dagli appunti tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
                : "Impossibile accedere agli appunti: riprova.";
        }

        private void DeleteHistoryEntry(HistoryEntryViewModel entry)
        {
            if (!_dialogs.Confirm("Eliminare dallo storico la voce " + entry.Title + " del " + entry.DateText + "?", "Storico"))
            {
                return;
            }

            _history.Remove(entry.Number);
            SaveHistory();
            RefreshHistory();
            StatusMessage = "Voce " + entry.Title + " eliminata.";
        }

        private void ClearHistory()
        {
            if (!_dialogs.Confirm("Cancellare tutto lo storico delle password?", "Storico"))
            {
                return;
            }

            ClearHistoryEntries();
            StatusMessage = "Storico cancellato.";
        }

        private void ClearHistoryEntries()
        {
            _history.Clear();
            SaveHistory();
            RefreshHistory();
        }

        private void SaveHistory()
        {
            try
            {
                if (_history.Entries.Count == 0)
                {
                    _historyStore.Delete();
                }
                else
                {
                    _historyStore.Save(_history);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Impossibile salvare lo storico: " + ex.Message;
            }
        }

        // ------------------------------------------------------------ Generazione

        private GenerationOptions BuildOptions()
        {
            return new GenerationOptions
            {
                Mode = _mode,
                WordCount = _wordCount,
                SyllableCount = _syllableCount,
                RandomLength = _randomLength,
                PreviousPassword = _previousPassword,
                PreviousPasswords = _historyEnabled ? _history.Passwords() : null,
                Policy = new PasswordPolicy
                {
                    MinLength = _minLength,
                    RequireUpper = _requireUpper,
                    RequireLower = _requireLower,
                    RequireDigit = _requireDigit,
                    RequireSpecial = _requireSpecial,
                    AvoidAmbiguous = _avoidAmbiguous
                }
            };
        }

        private void Generate()
        {
            try
            {
                var items = _generator.GenerateMany(BuildOptions(), _suggestionCount);
                IsChoosing = false;
                _lastCopied = null;
                Suggestions.Clear();
                foreach (var item in items)
                {
                    SuggestionViewModel row = null;
                    row = new SuggestionViewModel(item, new RelayCommand(() => Copy(row)));
                    Suggestions.Add(row);
                }

                if (!_loading)
                {
                    StatusMessage = string.IsNullOrEmpty(_previousPassword)
                        ? "Proposte generate."
                        : "Proposte generate, diverse dalla password attuale.";
                }
            }
            catch (InvalidOperationException ex)
            {
                Suggestions.Clear();
                StatusMessage = ex.Message;
            }
        }

        private void Copy(SuggestionViewModel suggestion)
        {
            _lastCopied = suggestion;
            StatusMessage = _clipboard.Copy(suggestion.Text)
                ? "Copiata negli appunti: verrà cancellata tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
                : "Impossibile accedere agli appunti: riprova.";
        }

        // ------------------------------------------------------------ Salvataggio

        /// <summary>Salva le preferenze (mai le password) in %AppData%\PasswordGen\settings.json.</summary>
        public void SaveSettings()
        {
            _settings.Mode = _mode;
            _settings.WordCount = _wordCount;
            _settings.SyllableCount = _syllableCount;
            _settings.RandomLength = _randomLength;
            _settings.SuggestionCount = _suggestionCount;
            _settings.MinLength = _minLength;
            _settings.RequireUpper = _requireUpper;
            _settings.RequireLower = _requireLower;
            _settings.RequireDigit = _requireDigit;
            _settings.RequireSpecial = _requireSpecial;
            _settings.AvoidAmbiguous = _avoidAmbiguous;
            _settings.HistoryEnabled = _historyEnabled;
            _settings.LockEnabled = _lockEnabled;
            _settings.LockGraceSeconds = _lockGraceSeconds;
            _settings.WordSource = _wordSource;
            _settings.CustomWordsPath = _customWordsPath;
            _settings.ReminderEnabled = _reminderEnabled;
            _settings.ValidityDays = _validityDays;

            try
            {
                _store.Save(_settings);
            }
            catch (Exception ex)
            {
                StatusMessage = "Impossibile salvare le impostazioni: " + ex.Message;
            }
        }
    }
}
