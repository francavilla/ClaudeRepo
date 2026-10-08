using System;
using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Mvvm;

namespace PasswordGen.ViewModels
{
    /// <summary>Una riga dello storico: la password è mascherata finché non viene mostrata.</summary>
    public sealed class HistoryEntryViewModel : ObservableObject
    {
        private const string Mask = "••••••••••••";

        private readonly HistoryEntry _entry;
        private bool _isRevealed;

        public HistoryEntryViewModel(HistoryEntry entry, Action<HistoryEntryViewModel> copy, Action<HistoryEntryViewModel> delete)
        {
            _entry = entry;
            ToggleCommand = new RelayCommand(() => IsRevealed = !IsRevealed, () => _entry.HasPassword);
            CopyCommand = new RelayCommand(() => copy(this), () => _entry.HasPassword);
            DeleteCommand = new RelayCommand(() => delete(this));
        }

        public int Number
        {
            get { return _entry.Number; }
        }

        public string Title
        {
            get { return "#" + _entry.Number; }
        }

        public string DateText
        {
            get
            {
                var date = _entry.Date;
                return date.HasValue ? date.Value.ToString("d", CultureInfo.CurrentCulture) : _entry.DateText;
            }
        }

        public string ModeText
        {
            get
            {
                if (!_entry.HasPassword)
                {
                    return "solo data";
                }

                switch (_entry.Mode)
                {
                    case GenerationMode.Syllables: return "sillabe";
                    case GenerationMode.Random: return "casuale";
                    default: return "parole";
                }
            }
        }

        public bool HasPassword
        {
            get { return _entry.HasPassword; }
        }

        public string Password
        {
            get { return _entry.Password; }
        }

        public bool IsRevealed
        {
            get { return _isRevealed; }
            private set
            {
                if (SetProperty(ref _isRevealed, value))
                {
                    OnPropertyChanged(nameof(DisplayText));
                    OnPropertyChanged(nameof(ToggleText));
                }
            }
        }

        /// <summary>Rimette la password sotto maschera (quando l'app si blocca).</summary>
        public void Hide()
        {
            IsRevealed = false;
        }

        public string DisplayText
        {
            get
            {
                if (!_entry.HasPassword)
                {
                    return "(registrata solo la data)";
                }

                return _isRevealed ? _entry.Password : Mask;
            }
        }

        public string ToggleText
        {
            get { return _isRevealed ? "Nascondi" : "Mostra"; }
        }

        public ICommand ToggleCommand { get; private set; }

        public ICommand CopyCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }
    }
}
