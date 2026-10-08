using System.Collections.ObjectModel;
using System.Windows.Input;
using PasswordGen.Core.Generation;
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

    public MainViewModel(PasswordGenerator generator, SettingsStore store, SecretClipboard clipboard)
    {
        _generator = generator;
        _store = store;
        _clipboard = clipboard;

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

        Suggestions = new ObservableCollection<SuggestionItem>();
        GenerateCommand = new Command(Generate);

        _loading = false;
        Generate();
    }

    public string Title => "PasswordGen " + AppInfo.Current.VersionString;

    public string[] ModeNames { get; } = { "Parole italiane", "Sillabe pronunciabili", "Caratteri casuali" };

    public ObservableCollection<SuggestionItem> Suggestions { get; }

    public ICommand GenerateCommand { get; }

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
