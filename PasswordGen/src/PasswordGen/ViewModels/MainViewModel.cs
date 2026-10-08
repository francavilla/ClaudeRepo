using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.Policy;
using PasswordGen.Core.Reminder;
using PasswordGen.Core.Settings;
using PasswordGen.Mvvm;
using PasswordGen.Services;

namespace PasswordGen.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly PasswordGenerator _generator;
        private readonly SettingsStore _store;
        private readonly ISecretClipboard _clipboard;
        private readonly IStartupRegistration _startup;
        private readonly Func<DateTime> _today;
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
        private bool _reminderEnabled;
        private int _validityDays;
        private bool _startWithWindows;
        private string _statusMessage = string.Empty;
        private string _reminderMessage = string.Empty;
        private string _reminderLevel = "Info";
        private string _lastChangeText = string.Empty;

        public MainViewModel(
            PasswordGenerator generator,
            SettingsStore store,
            ISecretClipboard clipboard,
            IStartupRegistration startup,
            Func<DateTime> today)
        {
            _generator = generator;
            _store = store;
            _clipboard = clipboard;
            _startup = startup;
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
            GenerateCommand = new RelayCommand(Generate);
            MarkChangedCommand = new RelayCommand(MarkChanged);

            _clipboard.Cleared += (s, e) => StatusMessage = "Appunti svuotati.";

            _loading = false;
            RefreshReminder();
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
                    OnPropertyChanged();
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

        private void MarkChanged()
        {
            _settings.LastChangeDate = _today();
            RefreshReminder();
            SaveSettings();
            StatusMessage = "Cambio password registrato: " + ReminderMessage;
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
