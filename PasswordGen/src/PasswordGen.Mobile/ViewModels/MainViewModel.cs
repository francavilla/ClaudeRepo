using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Settings;
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
    private bool _historyEnabled;
    private bool _reminderEnabled;
    private int _validityDays;
    private string _reminderMessage = string.Empty;
    private string _lastChangeText = string.Empty;
    private Color _reminderBackground = Color.FromArgb("#EFF6FF");

    /// <param name="historyStore">Archivio cifrato dello storico; nullo se la chiave non è disponibile (storico non utilizzabile).</param>
    public MainViewModel(
        PasswordGenerator generator,
        SettingsStore store,
        HistoryStore historyStore,
        SecretClipboard clipboard,
        IDialogService dialogs,
        IReminderScheduler scheduler)
    {
        _generator = generator;
        _store = store;
        _clipboard = clipboard;
        _historyStore = historyStore;
        _dialogs = dialogs;
        _scheduler = scheduler;

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

        HistoryAvailable = historyStore != null;
        _historyEnabled = HistoryAvailable && _settings.HistoryEnabled;
        _history = _historyEnabled ? historyStore.Load() : new PasswordHistory();

        Suggestions = new ObservableCollection<SuggestionItem>();
        HistoryEntries = new ObservableCollection<HistoryEntryItem>();
        GenerateCommand = new Command(Generate);
        MarkChangedCommand = new Command(() => RunSafe(MarkChangedAsync));
        ClearHistoryCommand = new Command(() => RunSafe(ClearHistoryAsync));

        _loading = false;
        RefreshReminder();
        RefreshHistory();
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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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
            Generate();
        }
    }

    public bool IsPassphrase => _mode == GenerationMode.Passphrase;

    public bool IsSyllables => _mode == GenerationMode.Syllables;

    public bool IsRandom => _mode == GenerationMode.Random;

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
            PreviousPasswords = _historyEnabled ? _history.Passwords() : null,
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
                row = new SuggestionItem(item, new Command(() => Copy(row)));
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
        StatusMessage = _clipboard.Copy(suggestion.Text)
            ? "Copiata negli appunti: verrà cancellata tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
            : "Impossibile accedere agli appunti: riprova.";
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
        ReminderBackground = Color.FromArgb(state.Status switch
        {
            ReminderStatus.Expired => "#FEE2E2",
            ReminderStatus.DueSoon => "#FEF3C7",
            ReminderStatus.Ok => "#DCFCE7",
            _ => "#DBEAFE",
        });
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

    public bool HistoryEnabled
    {
        get => _historyEnabled;
        set
        {
            if (value == _historyEnabled)
            {
                return;
            }

            if (!HistoryAvailable)
            {
                OnPropertyChanged();
                return;
            }

            if (!value && _history.Entries.Count > 0)
            {
                // Lo storico esistente verrebbe cancellato: serve conferma. Intanto l'interruttore torna com'era.
                OnPropertyChanged();
                RunSafe(DisableHistoryAsync);
                return;
            }

            _historyEnabled = value;
            OnPropertyChanged();
            SaveSettings();
            RefreshHistory();
        }
    }

    public ObservableCollection<HistoryEntryItem> HistoryEntries { get; }

    public bool HasHistory => _history.Entries.Count > 0;

    public string HistoryHeader => _history.Entries.Count > 0 ? "Storico (" + _history.Entries.Count + ")" : "Storico";

    private async Task DisableHistoryAsync()
    {
        if (!await _dialogs.ConfirmAsync("Disattivando lo storico, le password conservate vengono cancellate. Continuare?", "Storico"))
        {
            return;
        }

        _historyEnabled = false;
        ClearHistoryEntries();
        OnPropertyChanged(nameof(HistoryEnabled));
        SaveSettings();
    }

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

        var today = DateTime.Today;
        _settings.LastChangeDate = today;

        string message;
        if (_historyEnabled)
        {
            var entry = _history.Add(chosen?.Text, chosen?.Mode ?? _mode, today);
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
    }

    private async Task ClearHistoryAsync()
    {
        if (!await _dialogs.ConfirmAsync("Cancellare tutto lo storico delle password?", "Storico"))
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

    private void RefreshHistory()
    {
        HistoryEntries.Clear();
        foreach (var entry in _history.Entries)
        {
            HistoryEntries.Add(new HistoryEntryItem(entry, CopyHistoryEntry, item => RunSafe(() => DeleteHistoryEntryAsync(item))));
        }

        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HistoryHeader));
    }

    private void CopyHistoryEntry(HistoryEntryItem entry)
    {
        StatusMessage = _clipboard.Copy(entry.Password)
            ? "Password " + entry.Title + " copiata: verrà cancellata dagli appunti tra " + (int)_clipboard.ClearAfter.TotalSeconds + " secondi."
            : "Impossibile accedere agli appunti: riprova.";
    }

    private async Task DeleteHistoryEntryAsync(HistoryEntryItem entry)
    {
        if (!await _dialogs.ConfirmAsync("Eliminare dallo storico la voce " + entry.Title + " del " + entry.DateText + "?", "Storico"))
        {
            return;
        }

        _history.Remove(entry.Number);
        SaveHistory();
        RefreshHistory();
        StatusMessage = "Voce " + entry.Title + " eliminata.";
    }

    private void SaveHistory()
    {
        if (_historyStore == null)
        {
            return;
        }

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
        _settings.HistoryEnabled = _historyEnabled;
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
