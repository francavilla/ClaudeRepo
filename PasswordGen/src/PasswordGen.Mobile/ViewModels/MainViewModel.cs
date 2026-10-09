using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Security;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Settings;
using PasswordGen.Core.Sync;
using PasswordGen.Core.Sync.Google;
using System.Security.Cryptography;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly PasswordGenerator _generator;
    private readonly SettingsStore _store;
    private readonly SecretClipboard _clipboard;
    private readonly AppSettings _settings;
    private readonly HistoryStore _historyStore;
    private readonly PasswordHistory _history;
    private readonly IDialogService _dialogs;
    private readonly IReminderScheduler _scheduler;
    private readonly WordList _builtinWords;
    private readonly IWordFileService _wordFiles;
    private readonly ISecurityService _security;
    private readonly AppLockController _lock;
    private readonly SyncPassphraseStore _syncPassphrases;
    private readonly IDocumentService _documents;
    private readonly IGoogleDriveService _drive;
    private bool _syncBusy;
    private string _syncStatus = string.Empty;

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
    private string _statusMessage = string.Empty;
    private WordSourceMode _wordSource;
    private string _customWordsPath;
    private WordFileResult _customWords;
    private string _wordSummary = string.Empty;
    private string _wordWarning = string.Empty;
    private bool _lockEnabled;
    private int _lockGraceSeconds;
    private bool _historyEnabled;
    private bool _reminderEnabled;
    private int _validityDays;
    private string _reminderMessage = string.Empty;
    private string _lastChangeText = string.Empty;
    private bool _hasReminderDays;
    private string _reminderDaysText = string.Empty;
    private string _reminderDaysCaption = string.Empty;
    private double _reminderProgress;
    private Color _reminderProgressColor = AppPalette.Accent;
    private Color _reminderBackground = Color.FromArgb("#EFF6FF");

    /// <param name="syncPassphrases">Frase segreta della sincronizzazione (cifrata col Keystore); nulla se la chiave non è disponibile.</param>
    /// <param name="historyStore">Archivio cifrato dello storico; nullo se la chiave non è disponibile (storico non utilizzabile).</param>
    public MainViewModel(
        PasswordGenerator generator,
        WordList builtinWords,
        SettingsStore store,
        HistoryStore historyStore,
        SecretClipboard clipboard,
        IDialogService dialogs,
        IReminderScheduler scheduler,
        IWordFileService wordFiles,
        ISecurityService security,
        AppLockController appLock,
        SyncPassphraseStore syncPassphrases,
        IDocumentService documents,
        IGoogleDriveService drive)
    {
        _drive = drive;
        _syncPassphrases = syncPassphrases;
        _documents = documents;
        _generator = generator;
        _store = store;
        _clipboard = clipboard;
        _historyStore = historyStore;
        _dialogs = dialogs;
        _scheduler = scheduler;
        _builtinWords = builtinWords;
        _wordFiles = wordFiles;
        _security = security;
        _lock = appLock;

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
        _reminderEnabled = _settings.ReminderEnabled;
        _validityDays = _settings.ValidityDays;
        _wordSource = _settings.WordSource;
        _lockGraceSeconds = _settings.LockGraceSeconds;
        _syncIntervalSeconds = _settings.SyncIntervalSeconds;
        _lockEnabled = _settings.LockEnabled && (security.IsAvailable || appLock.HasCredential);
        _customWordsPath = _settings.CustomWordsPath;
        if (!string.IsNullOrEmpty(_customWordsPath))
        {
            _customWords = CustomWordList.Load(_customWordsPath);
        }

        HistoryAvailable = historyStore != null;
        _historyEnabled = HistoryAvailable;   // lo storico è sempre attivo (regola aziendale delle ultime 20 password)
        _history = _historyEnabled ? historyStore.Load() : new PasswordHistory();

        Suggestions = new ObservableCollection<SuggestionItem>();
        HistoryEntries = new ObservableCollection<HistoryEntryItem>();
        GenerateCommand = new Command(Generate);
        MarkChangedCommand = new Command(() => RunSafe(MarkChangedAsync));
        ClearHistoryCommand = new Command(() => RunSafe(ClearHistoryAsync));
        LoadWordFileCommand = new Command(() => RunSafe(LoadWordFileAsync));
        ResetWordsCommand = new Command(ResetWords, () => _customWords != null || _wordSource != WordSourceMode.Builtin);

        _loading = false;
        RefreshReminder();
        RefreshHistory();
        ApplyWordSource();
        Generate();
        if (!HistoryAvailable)
        {
            StatusMessage = "Lo storico non è disponibile: il Keystore di Android non ha dato la chiave di cifratura.";
        }
    }

    public string Title => "PasswordGen " + AppInfo.Current.VersionString;

    public string[] ModeNames { get; } = { "Parole italiane", "Sillabe pronunciabili", "Caratteri casuali" };

    public ObservableCollection<SuggestionItem> Suggestions { get; }

    public ICommand GenerateCommand { get; }

    public ICommand MarkChangedCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public ICommand LoadWordFileCommand { get; }

    public ICommand ResetWordsCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (!SetProperty(ref _statusMessage, value) || string.IsNullOrEmpty(value))
            {
                return;
            }

            // Il messaggio resta visibile per un po', poi la barra si svuota (altrimenti «Proposte generate» resterebbe per sempre).
            var version = ++_statusVersion;
            Application.Current?.Dispatcher?.DispatchDelayed(TimeSpan.FromSeconds(StatusSeconds), () =>
            {
                if (version == _statusVersion)
                {
                    StatusMessage = string.Empty;
                }
            });
        }
    }

    private const int StatusSeconds = 15;
    private int _statusVersion;

    /// <summary>Svuota la barra di stato (per esempio quando si cambia scheda).</summary>
    public void ClearStatus()
    {
        _statusVersion++;
        StatusMessage = string.Empty;
    }

    // ---------------------------------------------------------------- Tipo di password

    public int ModeIndex
    {
        get => (int)_mode;
        set
        {
            if (value < 0 || value > 2 || value == (int)_mode)
            {
                return;
            }

            _mode = (GenerationMode)value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPassphrase));
            OnPropertyChanged(nameof(IsSyllables));
            OnPropertyChanged(nameof(IsRandom));
            OnPropertyChanged(nameof(ModeDescription));
            OnPropertyChanged(nameof(ModeExample));
            OnPropertyChanged(nameof(PassphraseBackground));
            OnPropertyChanged(nameof(SyllablesBackground));
            OnPropertyChanged(nameof(RandomBackground));
            Generate();
        }
    }

    // Le tre voci di scelta sono pulsanti di opzione: l'impostazione a «vero» cambia il tipo; l'«falso» (quando un'altra voce viene scelta) si ignora.
    // Le tre voci sono riquadri da toccare (non RadioButton: con un contenuto composto MAUI mostrava il nome del tipo); il cerchio pieno e lo sfondo indicano la scelta.
    public ICommand SelectModeCommand => new Command<string>(index => ModeIndex = int.Parse(index));

    /// <summary>Da chiamare quando il telefono passa dal tema chiaro a quello scuro (o viceversa): ricalcola i colori scelti dal codice.</summary>
    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(PassphraseBackground));
        OnPropertyChanged(nameof(SyllablesBackground));
        OnPropertyChanged(nameof(RandomBackground));
        RefreshReminder();
    }

    public string ModeDescription => _mode switch
    {
        GenerationMode.Syllables => "Sillabe pronunciabili, facili da dire ad alta voce.",
        GenerationMode.Random => "Caratteri casuali, difficili da ricordare: pensata per quando basta copiarla.",
        _ => "Parole italiane, facili da ricordare (consigliata).",
    };

    public string ModeExample => _mode switch
    {
        GenerationMode.Syllables => "Bamelo-Tirusa-Pevono83=",
        GenerationMode.Random => "k7Q#mP2v!xR4tw9N",
        _ => "Lampo-Cavallo-Nebbia-Fiume47!",
    };

    public Color PassphraseBackground => ModeBackground(GenerationMode.Passphrase);

    public Color SyllablesBackground => ModeBackground(GenerationMode.Syllables);

    public Color RandomBackground => ModeBackground(GenerationMode.Random);

    private Color ModeBackground(GenerationMode mode)
    {
        return _mode == mode ? AppPalette.SelectedBackground : AppPalette.CardBackground;
    }

    public bool IsPassphrase
    {
        get => _mode == GenerationMode.Passphrase;
        set
        {
            if (value)
            {
                ModeIndex = (int)GenerationMode.Passphrase;
            }
        }
    }

    public bool IsSyllables
    {
        get => _mode == GenerationMode.Syllables;
        set
        {
            if (value)
            {
                ModeIndex = (int)GenerationMode.Syllables;
            }
        }
    }

    public bool IsRandom
    {
        get => _mode == GenerationMode.Random;
        set
        {
            if (value)
            {
                ModeIndex = (int)GenerationMode.Random;
            }
        }
    }

    // Gli slider lavorano con double: i valori sono sempre arrotondati a interi.
    public double WordCount
    {
        get => _wordCount;
        set => SetOption(ref _wordCount, (int)Math.Round(value));
    }

    public double SyllableCount
    {
        get => _syllableCount;
        set => SetOption(ref _syllableCount, (int)Math.Round(value));
    }

    public double RandomLength
    {
        get => _randomLength;
        set => SetOption(ref _randomLength, (int)Math.Round(value));
    }

    public double SuggestionCount
    {
        get => _suggestionCount;
        set => SetOption(ref _suggestionCount, (int)Math.Round(value));
    }

    // ---------------------------------------------------------------- Policy

    public double MinLength
    {
        get => _minLength;
        set
        {
            if (SetOption(ref _minLength, (int)Math.Round(value)))
            {
                OnPropertyChanged(nameof(PolicySummary));
            }
        }
    }

    public bool RequireUpper
    {
        get => _requireUpper;
        set => SetPolicyFlag(ref _requireUpper, value);
    }

    public bool RequireLower
    {
        get => _requireLower;
        set => SetPolicyFlag(ref _requireLower, value);
    }

    public bool RequireDigit
    {
        get => _requireDigit;
        set => SetPolicyFlag(ref _requireDigit, value);
    }

    public bool RequireSpecial
    {
        get => _requireSpecial;
        set => SetPolicyFlag(ref _requireSpecial, value);
    }

    public bool AvoidAmbiguous
    {
        get => _avoidAmbiguous;
        set => SetOption(ref _avoidAmbiguous, value);
    }

    public string PolicySummary
    {
        get
        {
            var parts = new List<string> { "almeno " + _minLength + " caratteri" };
            if (_requireUpper) { parts.Add("maiuscole"); }
            if (_requireLower) { parts.Add("minuscole"); }
            if (_requireDigit) { parts.Add("numeri"); }
            if (_requireSpecial) { parts.Add("caratteri speciali"); }
            return string.Join(", ", parts);
        }
    }

    /// <summary>Password attuale (facoltativa): resta solo in memoria. Le nuove proposte la evitano.</summary>
    public string PreviousPassword
    {
        get => _previousPassword;
        set => SetProperty(ref _previousPassword, value ?? string.Empty);
    }

    private void SetPolicyFlag(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        if (SetOption(ref field, value, name))
        {
            OnPropertyChanged(nameof(PolicySummary));
        }
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
            SaveSettings();
        }

        return true;
    }

    // ---------------------------------------------------------------- Generazione

    private GenerationOptions BuildOptions()
    {
        return new GenerationOptions
        {
            Mode = _mode,
            WordCount = _wordCount,
            SyllableCount = _syllableCount,
            RandomLength = _randomLength,
            PreviousPassword = _previousPassword,
            PreviousPasswords = _history.Passwords(),
            Policy = new PasswordPolicy
            {
                MinLength = _minLength,
                RequireUpper = _requireUpper,
                RequireLower = _requireLower,
                RequireDigit = _requireDigit,
                RequireSpecial = _requireSpecial,
                AvoidAmbiguous = _avoidAmbiguous,
            },
        };
    }

    private void Generate()
    {
        try
        {
            var items = _generator.GenerateMany(BuildOptions(), _suggestionCount);
            Suggestions.Clear();
            foreach (var item in items)
            {
                SuggestionItem row = null;
                row = new SuggestionItem(item, new Command(() => Copy(row)), Suggestions.Count == 0);
                Suggestions.Add(row);
            }

            if (!_loading)
            {
                StatusMessage = "Proposte generate.";
            }
        }
        catch (InvalidOperationException ex)
        {
            Suggestions.Clear();
            StatusMessage = ex.Message;
        }
    }

    private void Copy(SuggestionItem suggestion)
    {
        var copied = _clipboard.Copy(suggestion.Text);
        if (copied)
        {
            suggestion.ShowCopied();
        }

        StatusMessage = copied
            ? "Copiata negli appunti: verrà cancellata tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
            : "Impossibile accedere agli appunti: riprova.";
    }

    // ---------------------------------------------------------------- Sincronizzazione e backup

    private const string SyncFileName = "PasswordGen-sync.pgx";

    /// <summary>La sincronizzazione ha bisogno dello storico e della chiave del Keystore.</summary>
    public bool SyncAvailable => _syncPassphrases != null && HistoryAvailable;

    public bool SyncActive => SyncAvailable && !string.IsNullOrEmpty(_settings.SyncPath);

    public bool SyncNotActive => !SyncActive;

    public bool CanUseSync => SyncAvailable && !_syncBusy;

    public bool CanSyncNow => SyncActive && !_syncBusy;

    public string SyncSummary
    {
        get
        {
            if (!SyncAvailable)
            {
                return "Non disponibile: servono lo storico e la chiave del Keystore.";
            }

            if (!SyncActive)
            {
                return "Sincronizzazione non attiva.";
            }

            var last = ExchangeData.ParseTime(_settings.LastSyncUtcText);
            return "Attiva con " + SyncTargetName(_settings.SyncPath) + ". "
                + (last.HasValue ? "Ultima sincronizzazione: " + last.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) + "." : "Non ancora sincronizzato.");
        }
    }

    public ICommand SetupSyncCommand => new Command(() => RunSafe(SetupSyncAsync));

    public ICommand SyncNowCommand => new Command(() => RunSafe(SyncNowAsync));

    public ICommand StopSyncCommand => new Command(() => RunSafe(StopSyncAsync));

    public ICommand ExportCommand => new Command(() => RunSafe(ExportAsync));

    public ICommand ImportCommand => new Command(() => RunSafe(ImportAsync));

    private ISyncStorage OpenStorage(string address)
    {
        return address == _drive.Address ? _drive.CreateStorage() : _documents.Open(address);
    }

    private string SyncTargetName(string address)
    {
        return address == _drive.Address ? "il tuo Google Drive (file «PasswordGen-sync.pgx»)" : "il file «" + _documents.DisplayName(address) + "»";
    }

    private void RefreshSyncState()
    {
        OnPropertyChanged(nameof(SyncAvailable));
        OnPropertyChanged(nameof(SyncActive));
        OnPropertyChanged(nameof(SyncNotActive));
        OnPropertyChanged(nameof(CanUseSync));
        OnPropertyChanged(nameof(CanSyncNow));
        OnPropertyChanged(nameof(SyncSummary));
    }

    private void SetSyncBusy(bool busy)
    {
        _syncBusy = busy;
        OnPropertyChanged(nameof(CanUseSync));
        OnPropertyChanged(nameof(CanSyncNow));
    }

    private async Task SetupSyncAsync()
    {
        if (!_historyEnabled)
        {
            StatusMessage = "Per sincronizzare serve lo storico, che qui non è disponibile (manca la chiave del Keystore).";
            return;
        }

        var choice = await _dialogs.ChooseAsync("Dove sincronizzare?",
            new[]
            {
                "Il mio Google Drive (accesso con l'account Google)",
                "Un file esistente (per esempio quello creato dal PC)",
                "Un nuovo file (in una cartella a scelta)",
            });
        if (choice < 0)
        {
            return;
        }

        string address;
        if (choice == 0)
        {
            if (!_drive.IsSignedIn)
            {
                var problem = await _drive.SignInAsync();
                if (problem != null)
                {
                    StatusMessage = problem;
                    return;
                }
            }

            address = _drive.Address;
        }
        else
        {
            address = choice == 1 ? await _documents.PickExistingAsync() : await _documents.CreateAsync(SyncFileName);
        }

        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        var existing = await Task.Run(() =>
        {
            try
            {
                var bytes = OpenStorage(address).Read();
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        });

        var passphrase = await _dialogs.AskPassphraseAsync("Sincronizzazione",
            existing
                ? "Il file contiene già dati. Inserisci la frase segreta con cui è stato creato."
                : "Scegli una frase segreta per cifrare il file. Ti servirà anche sull'altro dispositivo e non si può recuperare.",
            !existing);
        if (passphrase == null)
        {
            return;
        }

        if (await RunSyncAsync(address, passphrase))
        {
            _syncPassphrases.Save(passphrase);   // prima la frase, poi l'indirizzo: appena la sincronizzazione risulta attiva, la frase c'è già
            _settings.SyncPath = address;
            SaveSettings();
            RefreshSyncState();
        }
    }

    private async Task SyncNowAsync()
    {
        if (_settings.SyncPath == _drive.Address && !_drive.IsSignedIn)
        {
            var problem = await _drive.SignInAsync();
            if (problem != null)
            {
                StatusMessage = problem;
                return;
            }
        }

        var passphrase = _syncPassphrases.Load()
            ?? await _dialogs.AskPassphraseAsync("Sincronizzazione", "Inserisci la frase segreta del file di sincronizzazione.", false);
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

    private DateTime _lastBackgroundSync = DateTime.MinValue;

    /// <summary>
    /// Sincronizza in silenzio se l'ultima sincronizzazione di questo tipo risale ad almeno <paramref name="minimumInterval"/> fa:
    /// serve quando l'app torna in primo piano e a intervalli regolari, così le modifiche fatte sull'altro dispositivo
    /// (per esempio l'azzeramento dello storico) arrivano senza riavviare l'app.
    /// </summary>
    public async Task SyncIfIdleAsync(TimeSpan minimumInterval)
    {
        if (!SyncActive || _syncBusy || DateTime.UtcNow - _lastBackgroundSync < minimumInterval)
        {
            return;
        }

        _lastBackgroundSync = DateTime.UtcNow;

        // Controllo leggero: se l'impronta del file non è cambiata dall'ultima sincronizzazione non c'è nulla da fare.
        var token = await ProbeAsync(_settings.SyncPath);
        if (token != null && token == _knownRemoteToken)
        {
            return;
        }

        // Dove non si può controllare l'impronta (per esempio un documento di Android) si sincronizza al massimo ogni due minuti.
        if (token == null && DateTime.UtcNow - _lastFullSync < TimeSpan.FromSeconds(Math.Max(120, _syncIntervalSeconds)))
        {
            return;
        }

        await AutoSyncAsync();
    }

    private static readonly int[] SyncIntervalChoices = { 15, 30, 60, 120, 300, 600 };

    public string[] SyncIntervalNames
    {
        get { return new[] { "Ogni 15 secondi", "Ogni 30 secondi", "Ogni minuto", "Ogni 2 minuti", "Ogni 5 minuti", "Ogni 10 minuti" }; }
    }

    /// <summary>Ogni quanto, ad app aperta, si controlla se l'altro dispositivo ha cambiato qualcosa.</summary>
    public TimeSpan SyncInterval
    {
        get { return TimeSpan.FromSeconds(_syncIntervalSeconds); }
    }

    public int SyncIntervalIndex
    {
        get
        {
            // Un valore salvato che non è tra le scelte (file modificato a mano) si porta alla più vicina.
            var best = 0;
            for (var i = 1; i < SyncIntervalChoices.Length; i++)
            {
                if (Math.Abs(SyncIntervalChoices[i] - _syncIntervalSeconds) < Math.Abs(SyncIntervalChoices[best] - _syncIntervalSeconds))
                {
                    best = i;
                }
            }

            return best;
        }
        set
        {
            if (value < 0 || value >= SyncIntervalChoices.Length || SyncIntervalChoices[value] == _syncIntervalSeconds)
            {
                return;
            }

            _syncIntervalSeconds = SyncIntervalChoices[value];
            OnPropertyChanged();
            SaveSettings();
        }
    }

    private DateTime _lastFullSync = DateTime.MinValue;
    private int _syncIntervalSeconds = AppSettings.DefaultSyncIntervalSeconds;
    private string _knownRemoteToken;

    private Task<string> ProbeAsync(string address)
    {
        return Task.Run(() =>
        {
            try
            {
                var probe = OpenStorage(address) as IChangeProbe;
                return probe == null ? null : probe.Probe();
            }
            catch (Exception)
            {
                return null;
            }
        });
    }

    private async Task<bool> RunSyncAsync(string address, string passphrase)
    {
        SetSyncBusy(true);
        try
        {
            SaveSettings();   // porta nelle impostazioni i valori correnti (per esempio la durata della password)
            var hadHistoryBefore = _history.Entries.Count > 0;
            var tokenBefore = await ProbeAsync(address);
            var result = await SyncEngine.RunAsync(OpenStorage(address), passphrase, _history, _settings, DateTime.UtcNow);
            _lastFullSync = DateTime.UtcNow;
            if (!result.Succeeded)
            {
                StatusMessage = result.Message;
                if (address == _drive.Address && result.Message != null && result.Message.Contains("accedi di nuovo"))
                {
                    _drive.SignOut();   // l'accesso non vale più: al prossimo «Sincronizza ora» si rifà l'accesso
                }

                return false;
            }

            _knownRemoteToken = result.Wrote ? null : tokenBefore;   // se abbiamo scritto, il prossimo controllo rilegge una volta
            var hadHistory = _history.Entries.Count > 0;
            _validityDays = _settings.ValidityDays;
            OnPropertyChanged(nameof(ValidityDays));
            SaveHistory();
            RefreshHistory();
            RefreshReminder();
            SaveSettings();
            StatusMessage = hadHistoryBefore && !hadHistory
                ? "Sincronizzato: lo storico è stato azzerato dall'altro dispositivo."
                : result.EntriesAdded > 0
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
            SetSyncBusy(false);
            RefreshSyncState();
        }
    }

    private async Task StopSyncAsync()
    {
        if (!await _dialogs.ConfirmAsync("Disattivare la sincronizzazione? Il file resta dov'è e i dati su questo telefono non cambiano.", "Sincronizzazione"))
        {
            return;
        }

        if (_settings.SyncPath == _drive.Address)
        {
            _drive.SignOut();
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
        if (!SyncAvailable)
        {
            StatusMessage = "L'esportazione non è disponibile: lo storico non è utilizzabile.";
            return;
        }

        var passphrase = await _dialogs.AskPassphraseAsync("Esporta lo storico",
            "Scegli la frase segreta che cifra il file: servirà per importarlo. Non si può recuperare.", true);
        if (passphrase == null)
        {
            return;
        }

        var address = await _documents.CreateAsync("PasswordGen-backup.pgx");
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        SetSyncBusy(true);
        try
        {
            SaveSettings();
            var history = _history.Clone();
            var settings = _settings.Clone();
            var storage = _documents.Open(address);
            await Task.Run(() => storage.Write(ExchangeFile.Export(history, settings, DateTime.UtcNow, passphrase)));
            StatusMessage = "Esportati " + history.Entries.Count + " cambi password in «" + _documents.DisplayName(address) + "».";
        }
        catch (Exception ex)
        {
            StatusMessage = "Esportazione non riuscita: " + ex.Message;
        }
        finally
        {
            SetSyncBusy(false);
        }
    }

    private async Task ImportAsync()
    {
        if (!_historyEnabled)
        {
            StatusMessage = "Per importare serve lo storico, che qui non è disponibile (manca la chiave del Keystore).";
            return;
        }

        var address = await _documents.PickExistingAsync();
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        var passphrase = await _dialogs.AskPassphraseAsync("Importa lo storico", "Inserisci la frase segreta del file.", false);
        if (passphrase == null)
        {
            return;
        }

        SetSyncBusy(true);
        try
        {
            var storage = _documents.Open(address);
            var data = await Task.Run(() =>
            {
                var bytes = storage.Read();
                if (bytes == null || bytes.Length == 0)
                {
                    throw new InvalidDataException("Il file è vuoto.");
                }

                return ExchangeFile.Decrypt(bytes, passphrase);
            });

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
            SetSyncBusy(false);
        }
    }

    // ---------------------------------------------------------------- Suggerimento per il blocco

    /// <summary>Invita ad attivare il blocco finché non è attivo o l'utente chiude l'avviso.</summary>
    public bool ShowLockHint => LockAvailable && !_lockEnabled && !_settings.LockHintDismissed;

    public ICommand DismissLockHintCommand => new Command(DismissLockHint);

    public ICommand EnableLockFromHintCommand => new Command(() =>
    {
        DismissLockHint();
        LockEnabled = true;
    });

    private void DismissLockHint()
    {
        _settings.LockHintDismissed = true;
        OnPropertyChanged(nameof(ShowLockHint));
        SaveSettings();
    }

    // ---------------------------------------------------------------- Blocco dell'app

    private static readonly int[] GraceSeconds = { 0, 30, 60, 300 };

    public string[] LockGraceNames { get; } = { "Subito", "Dopo 30 secondi", "Dopo 1 minuto", "Dopo 5 minuti" };

    /// <summary>True se si può usare il blocco: il telefono ha un blocco schermo (Android 9 o successivo) oppure si può impostare un PIN o una password dell'app.</summary>
    public bool LockAvailable => _security.IsAvailable || _lock.CredentialsSupported;

    public string LockHint => LockAvailable
        ? "L'app chiede impronta o volto, il PIN o la password scelti qui, oppure il PIN del telefono, all'apertura e dopo il tempo scelto in secondo piano. Gli screenshot e l'anteprima tra le app recenti vengono bloccati."
        : "Per usare il blocco imposta prima un PIN, una sequenza o un'impronta nelle impostazioni di sicurezza di Android (serve Android 9 o successivo).";

    /// <summary>Il pulsante «Blocca adesso» ha senso solo con il blocco attivo.</summary>
    public bool CanLockNow => _lockEnabled;

    public ICommand LockNowCommand => new Command(() => _lock.LockNow());

    /// <summary>Come si sblocca oltre all'impronta: PIN o password dell'app, se impostati.</summary>
    public string CredentialText
    {
        get
        {
            if (!_lock.HasCredential)
            {
                return _lock.CredentialsSupported
                    ? "Nessun PIN o password dell'app: si sblocca con l'impronta o il PIN del telefono."
                    : "Si sblocca con l'impronta o il PIN del telefono.";
            }

            return _lock.CredentialKind == CredentialKind.Pin
                ? "PIN dell'app impostato (si può sbloccare anche con impronta o PIN del telefono)."
                : "Password dell'app impostata (si può sbloccare anche con impronta o PIN del telefono).";
        }
    }

    public bool HasCredential => _lock.HasCredential;

    /// <summary>«Imposta» se non c'è ancora un PIN o una password dell'app, «Cambia» se c'è già.</summary>
    public string SetCredentialText => _lock.HasCredential ? "Cambia PIN o password" : "Imposta PIN o password";

    public bool CanSetCredential => _lockEnabled && _lock.CredentialsSupported;

    public ICommand SetCredentialCommand => new Command(() => RunSafe(SetCredentialAsync));

    public ICommand RemoveCredentialCommand => new Command(() => RunSafe(RemoveCredentialAsync));

    public bool LockEnabled
    {
        get => _lockEnabled;
        set
        {
            if (value == _lockEnabled)
            {
                return;
            }

            // L'interruttore resta com'era finché non è andato tutto a buon fine.
            OnPropertyChanged();
            RunSafe(() => ChangeLockAsync(value));
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

    private async Task ChangeLockAsync(bool enable)
    {
        if (!enable)
        {
            // Chi trova il telefono sbloccato non può togliere il blocco: serve confermare l'identità.
            if (!await _lock.ConfirmIdentityAsync())
            {
                StatusMessage = "Identità non confermata: il blocco è rimasto com'era.";
                return;
            }

            _lockEnabled = false;
            _lock.SetEnabled(false);
            _lock.ClearCredential();
            RefreshLockProperties();
            SaveSettings();
            StatusMessage = "Blocco dell'app disattivato (il PIN o la password dell'app sono stati rimossi).";
            return;
        }

        var phone = _security.IsAvailable;
        var options = new List<string>();
        var kinds = new List<CredentialKind?>();
        if (phone)
        {
            options.Add("Impronta o PIN del telefono");
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
            StatusMessage = LockHint;
            return;
        }

        var index = await _dialogs.ChooseAsync("Come vuoi sbloccare PasswordGen?", options);
        if (index < 0)
        {
            return;
        }

        // Con il blocco schermo del telefono si verifica subito che impronta e PIN funzionino, prima di attivare il blocco.
        if (phone)
        {
            var outcome = await _security.AuthenticateAsync("PasswordGen", "Conferma per attivare il blocco");
            if (!outcome.Success)
            {
                StatusMessage = "Autenticazione non riuscita (" + outcome.Reason + "): il blocco è rimasto com'era.";
                return;
            }
        }

        var kind = kinds[index];
        if (kind.HasValue)
        {
            var secret = await _dialogs.AskNewSecretAsync(kind.Value);
            if (secret == null)
            {
                StatusMessage = "Nessun PIN o password impostati: il blocco è rimasto com'era.";
                return;
            }

            await _lock.SetCredentialAsync(kind.Value, secret);
        }

        _lockEnabled = true;
        _lock.SetEnabled(true);
        RefreshLockProperties();
        SaveSettings();
        StatusMessage = "Blocco dell'app attivato: scatta all'apertura e " + LockGraceNames[LockGraceIndex].ToLowerInvariant() + " in secondo piano. Prova «Blocca adesso».";
    }

    /// <summary>Imposta o cambia il PIN o la password dell'app (con il blocco già attivo).</summary>
    private async Task SetCredentialAsync()
    {
        if (!_lockEnabled || !_lock.CredentialsSupported)
        {
            return;
        }

        if (!await _lock.ConfirmIdentityAsync())
        {
            StatusMessage = "Identità non confermata: nulla è cambiato.";
            return;
        }

        var index = await _dialogs.ChooseAsync("Che cosa vuoi impostare?", new[] { "PIN dell'app (4-12 cifre)", "Password dell'app" });
        if (index < 0)
        {
            return;
        }

        var kind = index == 0 ? CredentialKind.Pin : CredentialKind.Password;
        var secret = await _dialogs.AskNewSecretAsync(kind);
        if (secret == null)
        {
            return;
        }

        await _lock.SetCredentialAsync(kind, secret);
        RefreshLockProperties();
        StatusMessage = kind == CredentialKind.Pin ? "PIN dell'app impostato." : "Password dell'app impostata.";
    }

    private async Task RemoveCredentialAsync()
    {
        if (!_lock.HasCredential)
        {
            return;
        }

        // Senza il blocco schermo del telefono il PIN o la password dell'app sono l'unico modo per sbloccare: non si possono togliere.
        if (!_security.IsAvailable)
        {
            StatusMessage = "Il telefono non ha un blocco schermo: il PIN o la password dell'app sono l'unico modo di sbloccare e non si possono rimuovere (puoi cambiarli o disattivare il blocco).";
            return;
        }

        if (!await _lock.ConfirmIdentityAsync())
        {
            StatusMessage = "Identità non confermata: nulla è cambiato.";
            return;
        }

        _lock.ClearCredential();
        RefreshLockProperties();
        StatusMessage = "PIN o password dell'app rimossi: si sblocca con l'impronta o il PIN del telefono.";
    }

    private void RefreshLockProperties()
    {
        OnPropertyChanged(nameof(LockEnabled));
        OnPropertyChanged(nameof(CanLockNow));
        OnPropertyChanged(nameof(CanSetCredential));
        OnPropertyChanged(nameof(HasCredential));
        OnPropertyChanged(nameof(CredentialText));
        OnPropertyChanged(nameof(SetCredentialText));
        OnPropertyChanged(nameof(ShowLockHint));
    }

    // ---------------------------------------------------------------- Parole della passphrase

    public bool IsBuiltinWords
    {
        get => _wordSource == WordSourceMode.Builtin;
        set { if (value) { SetWordSource(WordSourceMode.Builtin); } }
    }

    public bool IsCombinedWords
    {
        get => _wordSource == WordSourceMode.Combined;
        set { if (value) { SetWordSource(WordSourceMode.Combined); } }
    }

    public bool IsCustomOnlyWords
    {
        get => _wordSource == WordSourceMode.CustomOnly;
        set { if (value) { SetWordSource(WordSourceMode.CustomOnly); } }
    }

    /// <summary>True se è stato caricato un file leggibile con almeno una parola valida.</summary>
    public bool HasCustomWords => _customWords != null && _customWords.Error == null && _customWords.Words.Count > 0;

    /// <summary>Lista in uso e quanto vale ogni parola in bit.</summary>
    public string WordSummary
    {
        get => _wordSummary;
        private set => SetProperty(ref _wordSummary, value);
    }

    public string CustomFileSummary => _customWords == null
        ? "Nessun file caricato."
        : _customWords.FileName + ": " + _customWords.Summary;

    public string WordWarning
    {
        get => _wordWarning;
        private set
        {
            if (SetProperty(ref _wordWarning, value))
            {
                OnPropertyChanged(nameof(HasWordWarning));
            }
        }
    }

    public bool HasWordWarning => _wordWarning.Length > 0;

    private void SetWordSource(WordSourceMode mode)
    {
        if (_wordSource == mode)
        {
            return;
        }

        _wordSource = mode;
        ApplyWordSource();
        Generate();
        SaveSettings();
    }

    private async Task LoadWordFileAsync()
    {
        var picked = await _wordFiles.PickAsync();
        if (picked == null)
        {
            return;
        }

        WordFileResult result;
        try
        {
            result = CustomWordList.Parse(File.ReadAllLines(picked.TempPath, Encoding.UTF8), picked.FinalPath);
        }
        catch (Exception ex)
        {
            _wordFiles.Discard(picked);
            StatusMessage = "Impossibile leggere il file: " + ex.Message;
            return;
        }

        if (result.Words.Count == 0)
        {
            _wordFiles.Discard(picked);
            StatusMessage = "Il file non contiene parole valide (una per riga, 4-9 lettere): " + result.Summary;
            return;
        }

        // Il file precedente resta intatto finché quello nuovo non è valido.
        var previousPath = _customWordsPath;
        _wordFiles.Commit(picked);
        if (previousPath != null && previousPath != picked.FinalPath)
        {
            _wordFiles.Delete(previousPath);
        }

        _customWords = result;
        _customWordsPath = picked.FinalPath;
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
        _wordFiles.Delete(_customWordsPath);
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

        var origin = selection.Effective switch
        {
            WordSourceMode.Combined => "Lista integrata + " + _customWords.FileName,
            WordSourceMode.CustomOnly => "Solo " + _customWords.FileName,
            _ => "Lista integrata",
        };

        WordSummary = origin + ": " + selection.List.Count + " parole, "
            + selection.BitsPerWord.ToString("0.0", CultureInfo.CurrentCulture) + " bit per parola";
        WordWarning = selection.Warning;

        OnPropertyChanged(nameof(IsBuiltinWords));
        OnPropertyChanged(nameof(IsCombinedWords));
        OnPropertyChanged(nameof(IsCustomOnlyWords));
        OnPropertyChanged(nameof(HasCustomWords));
        OnPropertyChanged(nameof(CustomFileSummary));
    }

    // ---------------------------------------------------------------- Promemoria

    public bool ReminderEnabled
    {
        get => _reminderEnabled;
        set
        {
            if (SetProperty(ref _reminderEnabled, value))
            {
                RefreshReminder();
                SaveSettings();
                RunSafe(() => ApplyReminderAsync(value));
            }
        }
    }

    /// <summary>Per quanti giorni è valida la password (14-90).</summary>
    public double ValidityDays
    {
        get => _validityDays;
        set
        {
            if (SetProperty(ref _validityDays, (int)Math.Round(value)))
            {
                RefreshReminder();
                SaveSettings();
            }
        }
    }

    public string ReminderMessage
    {
        get => _reminderMessage;
        private set => SetProperty(ref _reminderMessage, value);
    }

    public bool HasReminderDays
    {
        get => _hasReminderDays;
        private set => SetProperty(ref _hasReminderDays, value);
    }

    public string ReminderDaysText
    {
        get => _reminderDaysText;
        private set => SetProperty(ref _reminderDaysText, value);
    }

    public string ReminderDaysCaption
    {
        get => _reminderDaysCaption;
        private set => SetProperty(ref _reminderDaysCaption, value);
    }

    public double ReminderProgress
    {
        get => _reminderProgress;
        private set => SetProperty(ref _reminderProgress, value);
    }

    public Color ReminderProgressColor
    {
        get => _reminderProgressColor;
        private set => SetProperty(ref _reminderProgressColor, value);
    }

    public string LastChangeText
    {
        get => _lastChangeText;
        private set => SetProperty(ref _lastChangeText, value);
    }

    public Color ReminderBackground
    {
        get => _reminderBackground;
        private set => SetProperty(ref _reminderBackground, value);
    }

    private void RefreshReminder()
    {
        var last = _settings.LastChangeDate;
        var state = ChangeReminder.Evaluate(_reminderEnabled, last, _validityDays, _settings.WarnDays, DateTime.Today);

        ReminderMessage = state.Message;
        ReminderBackground = state.Status switch
        {
            ReminderStatus.Expired => AppPalette.ReminderExpired,
            ReminderStatus.DueSoon => AppPalette.ReminderDueSoon,
            ReminderStatus.Ok => AppPalette.ReminderOk,
            _ => AppPalette.ReminderInfo,
        };
        // Giorni alla scadenza e avanzamento (0 = appena cambiata, 1 = scaduta).
        var remaining = state.DaysRemaining;
        HasReminderDays = remaining.HasValue;
        if (remaining.HasValue)
        {
            var days = Math.Abs(remaining.Value);
            ReminderDaysText = days.ToString(CultureInfo.CurrentCulture);
            ReminderDaysCaption = remaining.Value < 0 ? (days == 1 ? "giorno di ritardo" : "giorni di ritardo")
                : (days == 1 ? "giorno rimasto" : "giorni rimasti");
            ReminderProgress = _validityDays > 0 ? Math.Min(1.0, Math.Max(0.0, 1.0 - (double)remaining.Value / _validityDays)) : 1.0;
        }
        else
        {
            ReminderProgress = 0;
        }

        ReminderProgressColor = state.Status switch
        {
            ReminderStatus.Expired => AppPalette.StrengthWeak,
            ReminderStatus.DueSoon => AppPalette.StrengthFair,
            ReminderStatus.Ok => AppPalette.StrengthGood,
            _ => AppPalette.Accent,
        };

        LastChangeText = last.HasValue
            ? "Ultimo cambio: " + last.Value.ToString("d", CultureInfo.CurrentCulture)
            : "Nessun cambio registrato";
    }

    private async Task ApplyReminderAsync(bool enabled)
    {
        if (enabled && !await _scheduler.EnsureNotificationPermissionAsync())
        {
            StatusMessage = "Notifiche non consentite: il promemoria resta visibile qui, ma per riceverlo attivale dalle impostazioni di Android.";
        }

        _scheduler.Apply(enabled);
    }

    /// <summary>Riattiva il controllo giornaliero all'avvio (sopravvive solo finché Android non lo cancella).</summary>
    public void RestoreReminder()
    {
        if (_reminderEnabled)
        {
            _scheduler.Apply(true);
        }
    }

    // ---------------------------------------------------------------- Cambio password e storico

    public bool HistoryAvailable { get; }

    public ObservableCollection<HistoryEntryItem> HistoryEntries { get; }

    /// <summary>Quante delle voci disponibili sono occupate, per esempio «7 di 20 password conservate».</summary>
    public string HistoryUsageText => _history.Entries.Count + " di " + PasswordHistory.MaxEntries + " password conservate";

    public double HistoryUsage => (double)_history.Entries.Count / PasswordHistory.MaxEntries;

    public bool HasHistory => _history.Entries.Count > 0;

    public string HistoryHeader => _history.Entries.Count > 0 ? "Storico (" + _history.Entries.Count + ")" : "Storico";

    /// <summary>Chiede quale proposta è stata usata come nuova password e la registra nello storico.</summary>
    private async Task MarkChangedAsync()
    {
        SuggestionItem chosen = null;
        if (_historyEnabled && Suggestions.Count > 0)
        {
            var options = new List<string> { "Nessuna: registra solo la data" };
            for (var i = 0; i < Suggestions.Count; i++)
            {
                options.Add((i + 1) + " - " + Suggestions[i].Text);
            }

            var index = await _dialogs.ChooseAsync("Quale proposta hai usato come nuova password?", options);
            if (index < 0)
            {
                return;
            }

            if (index > 0)
            {
                chosen = Suggestions[index - 1];
            }
        }

        // Titolo facoltativo: dove la uso (annullando o lasciando vuoto non si salva nessun titolo).
        string label = null;
        if (chosen != null)
        {
            label = await _dialogs.AskTextAsync("Titolo (facoltativo)", "Dove usi questa password? Puoi lasciarlo vuoto.", string.Empty);
        }

        // Regola aziendale: la nuova password non può essere una delle ultime 20 (confronto identico).
        var duplicate = chosen == null ? null : _history.Find(chosen.Text);
        if (duplicate != null)
        {
            StatusMessage = "Questa password coincide con la #" + duplicate.Number + " dello storico (" + duplicate.DateText + "): scegli un'altra proposta.";
            return;
        }

        var today = DateTime.Today;
        _settings.LastChangeDate = today;

        string message;
        if (_historyEnabled)
        {
            var entry = _history.Add(chosen?.Text, chosen?.Mode ?? _mode, today, DateTime.UtcNow, label);
            SaveHistory();
            RefreshHistory();
            message = chosen == null
                ? "Cambio registrato (#" + entry.Number + ", solo data)."
                : "Cambio registrato nello storico come #" + entry.Number + ".";
        }
        else
        {
            message = "Cambio password registrato.";
        }

        RefreshReminder();
        SaveSettings();
        StatusMessage = message + " " + ReminderMessage;
        _ = AutoSyncAsync();
    }

    private async Task ClearHistoryAsync()
    {
        if (!await _dialogs.ConfirmAsync("Azzerando lo storico l'app non potrà più controllare che la nuova password sia diversa dalle ultime 20 usate. "
                                         + "Fallo solo se cambi azienda o account. Azzerare lo storico?", "Azzera storico"))
        {
            return;
        }

        ClearHistoryEntries();
        StatusMessage = SyncActive ? "Storico azzerato: l'altro dispositivo lo azzererà alla prossima sincronizzazione." : "Storico azzerato.";
        _ = AutoSyncAsync();
    }

    private void ClearHistoryEntries()
    {
        _history.Reset(DateTime.UtcNow);   // l'azzeramento viaggia con la sincronizzazione
        SaveHistory();
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        HistoryEntries.Clear();
        foreach (var entry in _history.Entries)
        {
            HistoryEntries.Add(new HistoryEntryItem(entry, CopyHistoryEntry, item => RunSafe(() => EditHistoryLabelAsync(item)), HistoryEntries.Count == 0));
        }

        OnPropertyChanged(nameof(HistoryUsageText));
        OnPropertyChanged(nameof(HistoryUsage));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HistoryHeader));
    }

    private void CopyHistoryEntry(HistoryEntryItem entry)
    {
        var copied = _clipboard.Copy(entry.Password);
        if (copied)
        {
            entry.ShowCopied();
        }

        StatusMessage = copied
            ? "Password " + entry.Title + " copiata: verrà cancellata dagli appunti tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
            : "Impossibile accedere agli appunti: riprova.";
    }

    /// <summary>Aggiunge, cambia o toglie il titolo di una voce dello storico.</summary>
    private async Task EditHistoryLabelAsync(HistoryEntryItem entry)
    {
        var text = await _dialogs.AskTextAsync("Titolo della password " + entry.Title, "Dove usi questa password? (lascia vuoto per toglierlo)", entry.Label);
        if (text == null)
        {
            return;
        }

        if (_history.SetLabel(entry.Number, text, DateTime.UtcNow))
        {
            SaveHistory();
            RefreshHistory();
            StatusMessage = string.IsNullOrWhiteSpace(text) ? "Titolo tolto." : "Titolo salvato.";
            _ = AutoSyncAsync();
        }
    }

    private void SaveHistory()
    {
        if (_historyStore == null)
        {
            return;
        }

        try
        {
            if (_history.Entries.Count == 0 && _history.ResetUtcText == null)
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

    /// <summary>Da chiamare quando l'app va in secondo piano: toglie dalla memoria e dallo schermo i dati riservati.</summary>
    public void ClearSensitive()
    {
        PreviousPassword = string.Empty;
        foreach (var entry in HistoryEntries)
        {
            entry.IsRevealed = false;
        }
    }

    /// <summary>Esegue un'operazione asincrona dal comando di un pulsante, mostrando gli errori invece di far chiudere l'app.</summary>
    private async void RunSafe(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            StatusMessage = "Errore: " + ex.Message;
        }
    }

    // ---------------------------------------------------------------- Salvataggio

    /// <summary>Salva le preferenze (mai le password) nella cartella privata dell'app.</summary>
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
        _settings.LockEnabled = _lockEnabled;
        _settings.LockGraceSeconds = _lockGraceSeconds;
        _settings.SyncIntervalSeconds = _syncIntervalSeconds;
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
